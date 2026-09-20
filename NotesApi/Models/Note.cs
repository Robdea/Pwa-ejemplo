namespace NotesApi.Models;

public class Note
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// Cuerpo del endpoint de sincronización en bloque (POST /api/notes/sync).
/// Notas a insertar/actualizar y borrados pendientes.
/// </summary>
public class SyncRequest
{
    public List<Note> Notes { get; set; } = [];
    public List<Guid> DeletedIds { get; set; } = [];
}