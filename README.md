# Notas Demo

Aplicación pequeña para aprender cómo funciona una **PWA** con Blazor WebAssembly y sincronización con un servidor.

## Qué hace

- Añadir, editar y borrar notas.
- **Sin conexión:** las notas se guardan en tu navegador (localStorage) y todo sigue funcionando.
- **Con conexión:** los cambios se envían solos al servidor (PostgreSQL) y ya no se guardan en el navegador.
- Al volver la conexión, la sincronización es **automática**: no hay botones de "sincronizar".

## De qué está hecha

| Parte | Tecnología |
|-------|------------|
| App instalable (frontend) | Blazor WebAssembly, .NET 10 |
| Servidor (backend) | ASP.NET Core Web API, Entity Framework Core |
| Base de datos | PostgreSQL (con Docker) |
| Almacenamiento local | localStorage del navegador |

## Cómo ejecutarla

Necesitas: .NET 10, Docker Desktop y un navegador moderno.

```bash
# 1. Levantar la base de datos
docker compose up -d

# 2. Arrancar el servidor
dotnet run --project NotesApi

# 3. Arrancar la app (en otra terminal)
dotnet run --project NotesPwa
```

Abre `http://localhost:5001`.

## Probar el modo sin conexión

1. Crea algunas notas normalmente.
2. Para el servidor (Ctrl+C) o desconecta el wifi.
3. Crea, edita y borra notas: todo se guarda en el navegador y sigue funcionando.
4. Vuelve a encender el servidor o la conexión: todo lo que hiciste se sube solo.

## Ver los datos

- **En el navegador:** F12 → Application → Local Storage → clave `notes`.
- **En la base de datos:**
  ```bash
  docker exec -it notes-postgres psql -U postgres -d notes
  ```
  y después:
  ```sql
  SELECT "Id", "Title", "Text" FROM "Notes";
  ```

## Usarla desde otro dispositivo (red local)

```bash
# Servidor y app accesibles desde tu red
dotnet run --project NotesApi --urls http://0.0.0.0:5000
dotnet run --project NotesPwa --urls http://0.0.0.0:5001
```

Luego, en el archivo `NotesPwa/wwwroot/appsettings.json`, cambia `ApiBaseUrl` a tu IP local (por ejemplo `http://192.168.1.69:5000`) y desde el otro dispositivo entra en `http://TU-IP:5001`.

> Nota: los Service Workers (que permiten instalar la PWA) necesitan HTTPS fuera del `localhost`. En la red local funciona como web app normal con sincronización; para instalarla en otros dispositivos haría falta HTTPS.

---

**Versión 1.0.0**