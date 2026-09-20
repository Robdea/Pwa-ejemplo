using System.Net.Http.Json;
using NotesPwa.Models;

namespace NotesPwa.Services;

/// <summary>
/// Orquesta la sincronización automática entre el servidor y localStorage:
/// - En línea  -> los datos viven en el backend (localStorage no se usa).
/// - Sin línea -> los datos viven en localStorage.
/// Al volver la conexión se sube lo pendiente y se refresca desde el servidor.
/// </summary>
public class SyncService(HttpClient http, NoteService notes, LocalStorageService localStorage)
{
    public bool IsOnline { get; private set; }
    public bool IsSyncing { get; private set; }
    public bool HasPendingChanges { get; private set; }
    public string Status { get; private set; } = "Cargando…";

    private bool _initialized;

    public event Action? StateChanged;

    /// <summary>Al arrancar: intenta leer del servidor y, si no puede, usa localStorage.</summary>
    public async Task InitializeAsync()
    {
        await notes.LoadLocalAsync();

        if (await TryLoadFromServerAsync())
        {
            IsOnline = true;
            HasPendingChanges = false;
            Status = "En línea · datos del servidor";
        }
        else
        {
            IsOnline = false;
            HasPendingChanges = notes.Notes.Count > 0;
            Status = "Sin conexión · datos locales";
        }

        _initialized = true;
        StateChanged?.Invoke();
    }

    /// <summary>Cambia de estado cuando el backend aparece o desaparece.</summary>
    public async Task SetServerReachableAsync(bool reachable)
    {
        if (!_initialized)
        {
            return;
        }

        if (reachable && !IsOnline)
        {
            await SyncPendingAsync();
        }
        else if (!reachable && IsOnline)
        {
            await GoOfflineAsync();
        }
    }

    public async Task AddAsync(string title, string text)
    {
        var note = new Note { Id = Guid.NewGuid(), Title = title.Trim(), Text = text.Trim() };

        if (IsOnline && await TryCreateAsync(note))
        {
            notes.Add(note);
            Status = "Guardada en el servidor";
        }
        else
        {
            notes.Add(note);
            HasPendingChanges = true;
            await notes.SaveLocalAsync();
            await MaybeGoOfflineAsync();
            Status = "Guardada localmente (pendiente de sincronizar)";
        }

        StateChanged?.Invoke();
    }

    public async Task UpdateAsync(Note note)
    {
        if (IsOnline && await TryUpdateAsync(note))
        {
            notes.Update(note);
            Status = "Actualizada en el servidor";
        }
        else
        {
            notes.Update(note);
            HasPendingChanges = true;
            await notes.SaveLocalAsync();
            await MaybeGoOfflineAsync();
            Status = "Actualizada localmente (pendiente de sincronizar)";
        }

        StateChanged?.Invoke();
    }

    public async Task DeleteAsync(Guid id)
    {
        notes.Delete(id);

        if (IsOnline && await TryDeleteServerAsync(id))
        {
            Status = "Eliminada del servidor";
        }
        else
        {
            HasPendingChanges = true;
            await notes.SaveLocalAsync();
            await AddPendingDeleteAsync(id);
            await MaybeGoOfflineAsync();
            Status = "Eliminada localmente (pendiente de sincronizar)";
        }

        StateChanged?.Invoke();
    }

    private async Task SyncPendingAsync()
    {
        if (IsSyncing)
        {
            return;
        }

        IsSyncing = true;
        Status = "Sincronizando…";
        StateChanged?.Invoke();

        try
        {
            var request = new SyncRequest
            {
                Notes = notes.Notes.ToList(),
                DeletedIds = await localStorage.GetDeletedIdsAsync(),
            };

            var response = await http.PostAsJsonAsync("api/notes/sync", request);
            response.EnsureSuccessStatusCode();

            if (await TryLoadFromServerAsync())
            {
                IsOnline = true;
                HasPendingChanges = false;

                // En línea ya no se usa localStorage: se limpia la copia local.
                await localStorage.RemoveNotesAsync();
                await localStorage.RemoveDeletedIdsAsync();
                Status = "En línea · sincronizado con el servidor";
            }
            else
            {
                HasPendingChanges = true;
                Status = "Pendiente · no se pudo refrescar desde el servidor";
            }
        }
        catch
        {
            IsOnline = false;
            HasPendingChanges = true;
            Status = "Sin conexión · cambios pendientes";
        }
        finally
        {
            IsSyncing = false;
            StateChanged?.Invoke();
        }
    }

    private async Task GoOfflineAsync()
    {
        IsOnline = false;
        HasPendingChanges = true;

        // Se vuelca la lista actual (lo último visto del servidor) a localStorage.
        await notes.SaveLocalAsync();
        Status = "Sin conexión · datos locales";
        StateChanged?.Invoke();
    }

    private async Task<bool> TryLoadFromServerAsync()
    {
        try
        {
            var serverNotes = await http.GetFromJsonAsync<List<Note>>("api/notes") ?? [];
            notes.ReplaceAll(serverNotes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryCreateAsync(Note note)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/notes", note);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryUpdateAsync(Note note)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/notes/{note.Id}", note);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryDeleteServerAsync(Guid id)
    {
        try
        {
            var response = await http.DeleteAsync($"api/notes/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task MaybeGoOfflineAsync()
    {
        if (IsOnline)
        {
            IsOnline = false;
            StateChanged?.Invoke();
        }
    }

    private async Task AddPendingDeleteAsync(Guid id)
    {
        var ids = await localStorage.GetDeletedIdsAsync();
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }
        await localStorage.SetDeletedIdsAsync(ids);
    }
}