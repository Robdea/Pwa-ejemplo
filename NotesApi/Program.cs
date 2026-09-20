using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

// Ruta al publish de la PWA (NotesPwa/bil/Release/net10.0/publish). Si está
// configurada y existe, NotesApi sirve la PWA publicada junto a la API en el
// MISMO origen HTTPS → la app del móvil carga el service-worker y funciona
// sin conexión (se instala/confía el certificado una sola vez en el móvil).
string? pwaRoot = null;
var configuredPwaRoot = builder.Configuration.GetValue<string>("Pwa:Root");
if (!string.IsNullOrWhiteSpace(configuredPwaRoot) &&
    Directory.Exists(configuredPwaRoot))
{
    pwaRoot = configuredPwaRoot;
}

var app = builder.Build();

app.UseCors("NotesDemoCors");

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

if (pwaRoot is not null)
{
    var pwaFiles = new PhysicalFileProvider(pwaRoot);

    // Sirve el publish de la PWA (index.html + estáticos precacheados).
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = pwaFiles });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = pwaFiles,
        ServeUnknownFileTypes = true,
        OnPrepareResponse = ctx =>
        {
            // No cachear el HTML principal para que el SW precache nunca
            // se quede con una versión antigua en índices de la PWA.
            if (ctx.Context.Request.Path.Equals("/index.html",
                    StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.CacheControl = "no-cache";
            }
        },
    });

    // Fallback SPA: cualquier ruta que no exista → sirve index.html.
    // El service worker ya está publicado y se registra al servir por HTTPS.
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = pwaFiles,
        ServeUnknownFileTypes = true,
    });
}

app.MapControllers();

app.Run();
