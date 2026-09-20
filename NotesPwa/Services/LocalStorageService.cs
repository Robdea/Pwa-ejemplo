using System.Text.Json;
using Microsoft.JSInterop;
using NotesPwa.Models;

namespace NotesPwa.Services;

/// <summary>
/// Acceso directo a localStorage mediante JS interop.
/// Solo se usa cuando la aplicación trabaja fuera de línea.
/// </summary>
public class LocalStorageService(IJSRuntime js)
{
    public const string NotesKey = "notes";
    public const string DeletedIdsKey = "deletedIds";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<List<Note>?> GetNotesAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", NotesKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<Note>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // Datos de una versión anterior incompatibles: se descartan.
            return null;
        }
    }

    public async Task SetNotesAsync(List<Note> notes)
    {
        var json = JsonSerializer.Serialize(notes, JsonOptions);
        await js.InvokeVoidAsync("localStorage.setItem", NotesKey, json);
    }

    public Task RemoveNotesAsync() =>
        js.InvokeVoidAsync("localStorage.removeItem", NotesKey).AsTask();

    public async Task<List<Guid>> GetDeletedIdsAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", DeletedIdsKey);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task SetDeletedIdsAsync(List<Guid> ids)
    {
        var json = JsonSerializer.Serialize(ids, JsonOptions);
        await js.InvokeVoidAsync("localStorage.setItem", DeletedIdsKey, json);
    }

    public Task RemoveDeletedIdsAsync() =>
        js.InvokeVoidAsync("localStorage.removeItem", DeletedIdsKey).AsTask();
}