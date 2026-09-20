namespace NotesPwa.Models;

public class Note
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// Cuerpo del POST /api/notes/sync: notas locales y borrados pendientes.
/// </summary>
public class SyncRequest
{
    public List<Note> Notes { get; set; } = [];
    public List<Guid> DeletedIds { get; set; } = [];
}