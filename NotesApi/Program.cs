using Microsoft.EntityFrameworkCore;
using NotesApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<NotesDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("NotesDb"),
        npgsql => npgsql.EnableRetryOnFailure()));

// Orígenes permitidos para la PWA (se configuran en appsettings.json).
// Para usar la app desde otro dispositivo en la red local, añade el origen
// de tu IP de LAN a Cors:AllowedOrigins o usa "*" (acepta cualquier origen).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5001"];

builder.Services.AddCors(options =>
    options.AddPolicy("NotesDemoCors", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    }));

var app = builder.Build();

// Aplica las migraciones de EF Core y crea la base de datos si no existe.
// Reintenta mientras PostgreSQL termina de arrancar (docker compose up -d).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotesDbContext>();

    const int maxAttempts = 15;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex,
                "PostgreSQL no está listo (intento {Attempt} de {Max}). Reintentando…",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.UseCors("NotesDemoCors");

app.MapControllers();

app.Run();