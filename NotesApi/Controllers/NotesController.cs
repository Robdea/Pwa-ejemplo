using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Models;

namespace NotesApi.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController(NotesDbContext db) : ControllerBase
{
    // GET /api/notes -> todas las notas
    [HttpGet]
    public async Task<ActionResult<List<Note>>> GetNotes()
    {
        var notes = await db.Notes.AsNoTracking().OrderBy(n => n.Title).ThenBy(n => n.Id).ToListAsync();
        return Ok(notes);
    }

    // POST /api/notes -> crear una nota (el Id lo genera el cliente, es un Guid)
    [HttpPost]
    public async Task<ActionResult<Note>> Create(Note? note)
    {
        if (note is null)
        {
            return BadRequest("El cuerpo de la petición es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(note.Title))
        {
            return BadRequest("El título es obligatorio.");
        }

        note.Title = note.Title.Trim();
        note.Text = note.Text?.Trim() ?? "";
        note.Id = note.Id == Guid.Empty ? Guid.NewGuid() : note.Id;

        if (await db.Notes.AnyAsync(n => n.Id == note.Id))
        {
            return Conflict("Ya existe una nota con ese Id.");
        }

        db.Notes.Add(note);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetNotes), new { id = note.Id }, note);
    }

    // PUT /api/notes/{id} -> actualizar una nota
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, Note? note)
    {
        if (note is null)
        {
            return BadRequest("El cuerpo de la petición es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(note.Title))
        {
            return BadRequest("El título es obligatorio.");
        }

        var existing = await db.Notes.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Title = note.Title.Trim();
        existing.Text = note.Text?.Trim() ?? "";
        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/notes/{id} -> eliminar una nota
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var existing = await db.Notes.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        db.Notes.Remove(existing);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/notes/sync -> sincronización en bloque (replay del trabajo offline)
    [HttpPost("sync")]
    public async Task<ActionResult> Sync(SyncRequest? request)
    {
        if (request is null)
        {
            return BadRequest("El cuerpo de la petición es obligatorio.");
        }

        request.Notes ??= [];
        request.DeletedIds ??= [];

        foreach (var incoming in request.Notes)
        {
            if (incoming is null)
            {
                continue;
            }

            if (incoming.Id == Guid.Empty)
            {
                continue;
            }

            var existing = await db.Notes.FindAsync(incoming.Id);
            if (existing is null)
            {
                db.Notes.Add(new Note
                {
                    Id = incoming.Id,
                    Title = (incoming.Title ?? "").Trim(),
                    Text = (incoming.Text ?? "").Trim(),
                });
            }
            else
            {
                existing.Title = (incoming.Title ?? "").Trim();
                existing.Text = (incoming.Text ?? "").Trim();
            }
        }

        foreach (var id in request.DeletedIds)
        {
            var existing = await db.Notes.FindAsync(id);
            if (existing is not null)
            {
                db.Notes.Remove(existing);
            }
        }

        await db.SaveChangesAsync();

        return Ok(new { Sincronizadas = request.Notes.Count, Eliminadas = request.DeletedIds.Count });
    }
}