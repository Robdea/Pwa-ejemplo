using NotesPwa.Models;

namespace NotesPwa.Services;

/// <summary>
/// Lista de notas en memoria (lo que ve la interfaz).
/// La persistencia con localStorage la decide el SyncService según haya o no conexión.
/// </summary>
public class NoteService(LocalStorageService localStorage)
{
    private readonly List<Note> _notes = [];

    public IReadOnlyList<Note> Notes => _notes;

    public void Add(Note note) => _notes.Add(note);

    public void Update(Note note)
    {
        var existing = _notes.FirstOrDefault(n => n.Id == note.Id);
        if (existing is null)
        {
            return;
        }

        existing.Title = note.Title;
        existing.Text = note.Text;
    }

    public void Delete(Guid id) => _notes.RemoveAll(n => n.Id == id);

    public void ReplaceAll(IEnumerable<Note> notes)
    {
        _notes.Clear();
        _notes.AddRange(notes);
    }

    /// <summary>Carga las notas guardadas en localStorage.</summary>
    public async Task LoadLocalAsync()
    {
        var saved = await localStorage.GetNotesAsync();
        if (saved is null)
        {
            return;
        }

        _notes.Clear();
        _notes.AddRange(saved);
    }

    /// <summary>Guarda la lista actual en localStorage (modo sin conexión).</summary>
    public Task SaveLocalAsync() => localStorage.SetNotesAsync(_notes);
}