# NotesDemo — Blazor WASM PWA + ASP.NET Core API + PostgreSQL

App de notas tipo Blazor WebAssembly PWA, con una API .NET que publica la PWA
**en el mismo origen HTTPS** y una base de datos PostgreSQL. Diseñada para
probarla **desde el móvil en tu red local (LAN)** y que funcione **sin conexión**.

## Arquitectura

| Pieza      | Tecnología                                                     |
|------------|----------------------------------------------------------------|
| `NotesApi` | ASP.NET Core Web API (.NET 10) + EF Core + Npgsql              |
| `NotesPwa` | Blazor WebAssembly PWA (.NET 10), `dotnet publish` precachea el service worker |
| `NotesDb`  | PostgreSQL 17 (Docker Compose)                                 |
| Certificado| PFX autofirmado con **IPAddress 192.168.1.69** como SAN (HTTPS LAN) |

**Clave de diseño:** la API sirve también el `publish\wwwroot` de la PWA en el
mismo origen HTTPS (`https://<tu-IP>:5000`). Así:

- **Un solo certificado** que confiar en el móvil (sin CORS ni mixed-content).
- El **service worker se registra y precachea** en la primera carga → la PWA
  funciona **offline** después.

## Prueba en el móvil (LAN)

1. Enciende el PC y pon en marcha los servicios (ver abajo).
2. En el móvil abre: **`https://192.168.1.69:5000`**
   - Como el certificado es autofirmado, el móvil te avisará: **acepta /
     Continuar** (una sola vez). No hace falta instalar nada más.
3. La primera carga precachea la PWA. A partir de ahí, cierra el navegador:
   la app abre **sin conexión** desde el icono/escritorio.
4. Las notas se guardan en PostgreSQL a través de `/api/notes`.

> Si `192.168.1.69` no es tu IP de LAN (cambia por DHCP), regenera el
> certificado y actualiza la URL:
> `powershell -ExecutionPolicy Bypass -File tools\gen-cert.ps1`
> (luego reanuda API + publica la PWA y ajusta `ApiBaseUrl`).

## Puesta en marcha

```powershell
# 1. Base de datos
docker compose up -d

# 2. API + PWA (sirve todo junto en HTTPS 0.0.0.0:5000)
dotnet run --project NotesApi -c Release --no-build --urls https://0.0.0.0:5000

# 3. Publicar de nuevo la PWA si cambia el código (precache del SW)
dotnet publish NotesPwa -c Release -o NotesPwa\bin\Release\net10.0\publish
```

## Configuración relevante

- `NotesApi/appsettings.json` → `Kestrel` (cert PFX + contraseña), `Pwa:Root`
  (publish de la PWA), `Cors:AllowedOrigins`, `ConnectionStrings:NotesDb`.
- `NotesPwa/wwwroot/appsettings.json` → `ApiBaseUrl` (URL pública de la API).

## Scripts

- `tools/gen-cert.ps1` — genera/regenera el PFX con SAN IP + lo instala en
  `CurrentUser\My`, exporta `certs/notes-lan.pfx` y `certs/notes-lan.cer`.
- `certs/` — certificado (PFX + CER) ya generado para `192.168.1.69`.
