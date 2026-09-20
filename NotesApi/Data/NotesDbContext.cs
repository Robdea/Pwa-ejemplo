using Microsoft.EntityFrameworkCore;
using NotesApi.Models;

namespace NotesApi.Data;

public class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("Notes");

            // El Id llega siempre desde el cliente (localStorage), por lo que
            // la base de datos no debe generarlo automáticamente.
            entity.Property(n => n.Id).ValueGeneratedNever();
        });
    }
}