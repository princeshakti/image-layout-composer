# Image Layout Composer

A full-stack image grid composition tool. Upload photos, pick a layout, and download a clean composed grid image.

- **Backend** — ASP.NET Core 8 Web API
- **Frontend** — React 18 + Vite single-page app

---

## Prerequisites

| Tool | Purpose |
|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Run the API |
| [Node.js 18+](https://nodejs.org/) | Run the frontend |
| [Docker](https://www.docker.com/get-started) | Optional — containerised API |

---

## Quick Start (recommended)

Run the API and frontend side by side in two terminals.

**Terminal 1 — API:**
```bash
cd ImageLayoutComposer
dotnet run
```
API starts on `http://localhost:5000`. Swagger UI available at `http://localhost:5000/swagger`.

**Terminal 2 — Frontend:**
```bash
cd frontend
npm install
npm run dev
```
Frontend starts on `http://localhost:3000`.

Open **`http://localhost:3000`** in your browser. The Vite dev server proxies all `/api` and `/outputs` requests from port 3000 to port 5000, so there are no CORS issues.

---

## Running the API (options)

### Option A — dotnet CLI
```bash
cd ImageLayoutComposer
dotnet run
```
API on **`http://localhost:5000`**. Swagger UI on **`http://localhost:5000/swagger`** (Development only).

### Option B — Docker Compose
```bash
docker compose up --build
```
API on **`http://localhost:8080`**. Runs in Production mode — Swagger UI and CORS are not exposed. Uploaded images and composed outputs persist in named Docker volumes (`uploads_data`, `outputs_data`) across container restarts.

### Option C — Docker (manual)
```bash
docker build -t image-layout-composer .
docker run -p 8080:8080 image-layout-composer
```

> **Note:** When running via Docker the API is on port **8080**. Update `vite.config.js` proxy target and the Postman `baseUrl` variable accordingly.

---

## Running the Frontend

```bash
cd frontend
npm install   # first time only
npm run dev
```

Open **`http://localhost:3000`**.

### Frontend workflow

**Step 1 — Upload:** Drag and drop or click to browse. Supports JPG and PNG, shows thumbnails, limits to 16 files. Zero-byte files and unsupported types are rejected with a clear error message.

**Step 2 — Compose:** Choose a layout (2×2, 3×3, 4×4), output format (PNG or JPEG), cell size (50–2000 px), and padding (0–100 px). A warning appears if your image count will cause the grid to grow extra rows. Each session can only be composed once — a 409 response is returned if you attempt to compose again.

**Step 3 — Download:** The composed image renders as an inline preview. A stats row shows the grid dimensions, placed image count, and empty cells. Click **Download image** to save.

---

## Testing with Postman

1. Open Postman and click **Import**
2. Select `examples/ImageLayoutComposer.postman_collection.json`
3. Run the requests in order: **1 Upload → 2 Compose → 3 Download**

`sessionId` and `outputFileName` are saved automatically as collection variables — no copy-pasting needed. The **Error Cases** folder can be run independently at any time.

If running via Docker change the `baseUrl` collection variable from `http://localhost:5000` to `http://localhost:8080`.

> **Note:** Each session can only be composed once. Running the Compose request a second time on the same session returns `409 Conflict`. The Error Cases folder includes a dedicated test for this. Upload fresh images to start a new session.

---

## Running the Tests

```bash
cd ImageLayoutComposer.Tests
dotnet test
```

22 tests across two files:

**`ImageComposerServiceTests` (unit tests — no HTTP):**
- All three layouts produce correct column/row counts and write an output file
- PNG output has correct extension and magic bytes (`89 50 4E 47`)
- JPEG output has correct extension and magic bytes (`FF D8`)
- Canvas pixel dimensions match: `cols × cellSize + (cols + 1) × padding`
- More images than the square capacity adds rows; all images placed, 0 dropped
- An odd image count leaves exactly 1 white cell on the last row
- Passing zero images throws `ArgumentException`
- Passing an out-of-range `cellSize` throws `ArgumentOutOfRangeException`
- Passing an out-of-range `padding` throws `ArgumentOutOfRangeException`

**`ApiIntegrationTests` (integration tests — full HTTP pipeline):**
- Uploading valid PNG + JPEG returns 200 with a `sessionId` and an `imageId` per file
- Uploading with no files returns 400
- Uploading an unsupported file type returns 400
- Uploading a spoofed extension (wrong magic bytes) returns 400
- Uploading a zero-byte file returns 400
- Uploading a mix of valid and zero-byte files returns 400
- Uploading 17 files (over the 16-file limit) returns 400
- Composing with all three layouts returns 200 with correct `gridColumns` and `gridRows`
- Composing the same session twice returns 409 on the second call
- Composing with an unknown `sessionId` returns 404
- Composing with an invalid `cellSize` returns 400
- Downloading a composed PNG returns 200 with `Content-Type: image/png`
- Downloading a composed JPEG returns 200 with `Content-Type: image/jpeg`
- Downloading a non-existent file returns 404

---

## API Reference

All responses use `Content-Type: application/json`. Errors always have the shape:
```json
{ "error": "Human-readable message" }
```

### POST `/api/images/upload`

Upload one or more images. Returns a unique `imageId` for each accepted image and a `sessionId` that groups the batch.

**Request:** `multipart/form-data`, field name `files` (repeat for multiple files).

| Constraint | Limit |
|---|---|
| Accepted formats | JPG / JPEG / PNG |
| Max file size | 20 MB per file |
| Max total per request | 80 MB |
| Max files per request | 16 |
| Zero-byte files | Rejected with 400 |

Files are validated twice — by extension against a shared allowed-extension list, then by actual magic bytes — so renamed files with spoofed extensions are rejected.

**Response `200 OK`:**
```json
{
  "sessionId": "a3f8e1c0b2d74e5f9a1c3e5b7d9f0123",
  "images": [
    {
      "imageId": "9b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e",
      "originalFileName": "sample1.png",
      "storedFileName": "9b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e.png",
      "fileSizeBytes": 8210,
      "mimeType": "image/png"
    }
  ],
  "message": "1 image(s) uploaded. Use sessionId with POST /api/images/{sessionId}/compose."
}
```

---

### GET `/api/images/{sessionId}`

List the images uploaded under a session.

---

### POST `/api/images/{sessionId}/compose`

Compose the session's images into a grid and return a download URL.

**Each session can only be composed once.** A second call on the same session returns `409 Conflict`. Upload new images to get a fresh session.

| Parameter | Type | Default | Description |
|---|---|---|---|
| `layout` | `Grid2x2` \| `Grid3x3` \| `Grid4x4` | `Grid2x2` | Grid column count |
| `cellSize` | int (50–2000) | `400` | Side length of each cell in pixels |
| `padding` | int (0–100) | `10` | Gap between cells in pixels |
| `format` | `Png` \| `Jpeg` | `Png` | Output image format |

**Grid behaviour:** Column count is fixed by `layout`. Rows grow to fit all images — no image is ever dropped and no grey placeholder cells are used. When extra rows are added a `warning` field explains this in the response.

**Response `200 OK`:**
```json
{
  "outputFileName": "grid_Grid2x2_20240315_143022000.png",
  "downloadUrl": "/outputs/grid_Grid2x2_20240315_143022000.png",
  "layout": "Grid2x2",
  "gridColumns": 2,
  "gridRows": 2,
  "totalImages": 4,
  "placedImages": 4,
  "emptyCells": 0,
  "warning": null
}
```

**Response `409 Conflict` (session already composed):**
```json
{
  "error": "Session 'abc123...' has already been composed. Upload new images to start a new session."
}
```

---

### GET `/api/images/download/{fileName}`

Download a composed image. Returns the file streamed from disk with the correct `Content-Type` (`image/png` or `image/jpeg`). The image is also accessible as a static file via the `downloadUrl` path returned from compose.

---

## Project Structure

```
ImageLayoutComposer/                   ← .NET API
├── Controllers/
│   └── ImagesController.cs            # HTTP endpoints, input validation (reads AllowedExtensions)
├── Models/
│   └── ApiModels.cs                   # Request/response types, GridLayout and OutputFormat enums
├── Services/
│   ├── StorageService.cs              # ConcurrentDictionary session store, TTL eviction,
│   │                                  #   magic-byte validation, AllowedExtensions (single source of truth)
│   ├── ImageComposerService.cs        # Grid composition logic (ImageSharp), service-level validation
│   └── SessionCleanupService.cs       # BackgroundService — evicts sessions older than 1 hour
├── Properties/
│   └── launchSettings.json            # Pins dotnet run to port 5000
├── Program.cs                         # DI registration, CORS + Swagger (Dev only), middleware
├── appsettings.json
└── wwwroot/
    ├── uploads/                       # Uploaded source images (per-session folders)
    └── outputs/                       # Composed grid output images

ImageLayoutComposer.Tests/             ← Test project
├── ApiIntegrationTests.cs             # End-to-end HTTP tests (WebApplicationFactory) — 13 tests
├── ImageComposerServiceTests.cs       # Unit tests for composition logic — 9 tests
└── TestImageFactory.cs                # Helpers: CreatePng, CreateJpeg, BuildMultipart,
                                       #   BuildRawPart, BuildEmptyPart

Frontend/                              ← React + Vite frontend
├── index.html
├── vite.config.js                     # Dev server + /api proxy to port 5000
├── package.json
└── src/
    ├── main.jsx
    ├── App.jsx                        # Root component, single state object for step/session/result
    ├── App.module.css
    ├── compose.config.js              # Single source of truth: LAYOUTS, COMPOSE_DEFAULTS, getCapacity
    ├── api/
    │   └── imageApi.js                # All fetch calls (uploadImages, composeGrid, downloadImage)
    ├── hooks/
    │   ├── useAsync.js                # Shared try/catch/finally loading pattern
    │   ├── useUpload.js               # File list state + upload API call (uses useAsync)
    │   └── useCompose.js              # Settings state + compose API call (uses useAsync)
    ├── components/
    │   ├── StepIndicator.jsx          # 1 / 2 / 3 step pill bar
    │   ├── Alert.jsx                  # Error / warning / success banners
    │   ├── Spinner.jsx                # Inline loading spinner
    │   ├── LoadingButton.jsx          # Shared spinner + label button (used by all three steps)
    │   ├── DropZone.jsx               # Drag-and-drop / click-to-browse file input
    │   ├── FileList.jsx               # Selected files with thumbnails (useEffect object URL, no leaks)
    │   ├── ComposeSettings.jsx        # Layout, format, cellSize, padding fields (reads LAYOUTS)
    │   ├── ResultStats.jsx            # Stat tiles showing grid dimensions after compose
    │   ├── UploadStep.jsx             # Step 1 page
    │   ├── ComposeStep.jsx            # Step 2 page
    │   └── DownloadStep.jsx           # Step 3 page (uses useAsync directly)
    └── styles/
        └── global.css                 # CSS variables and base reset

Examples/
├── ImageLayoutComposer.postman_collection.json
└── jpg files

Dockerfile                             # Two-stage build (SDK → runtime image)
docker-compose.yml                     # One-command startup, Production environment
```

---

## Dependencies

**API:**

| Package | Purpose |
|---|---|
| `SixLabors.ImageSharp` 3.x | Image loading, resizing (Lanczos3), compositing, encoding |
| `Swashbuckle.AspNetCore` 6.x | Swagger / OpenAPI UI (Development only) |
| `Microsoft.AspNetCore.Mvc.Testing` | In-process integration test host (tests only) |

**Frontend:**

| Package | Purpose |
|---|---|
| `react` / `react-dom` 18 | UI framework |
| `vite` + `@vitejs/plugin-react` | Dev server, HMR, production build |

---

## Assumptions & Tradeoffs

**Sessions are in-memory with TTL eviction.** The session store uses a `ConcurrentDictionary` — thread-safe without manual locks. A background `SessionCleanupService` runs every 5 minutes and evicts sessions older than 1 hour, deleting their upload files from disk. Sessions are still lost on process restart; persist to a database for durability.

**Each session can only be composed once.** `TryMarkComposed` is an atomic compare-and-swap using `Interlocked.CompareExchange`. This prevents a session from being composed multiple times, which would otherwise fill disk with duplicate output files. To compose again, upload new images.

**Download streams from disk.** The download endpoint uses `PhysicalFile` instead of `ReadAllBytes`, streaming the file directly without allocating a managed byte array. This avoids Large Object Heap pressure for large grid images (e.g. Grid4×4 at max cell size).

**CORS and Swagger are Development-only.** In Production (including Docker), the wildcard CORS policy and Swagger UI are not activated. Docker uses `ASPNETCORE_ENVIRONMENT=Production`.

**Single source of truth for allowed extensions.** `LocalStorageService.AllowedExtensions` is the only place where accepted file types are defined. The controller's validation reads from it, so adding a new format requires a change in exactly one place.

**Dual-layer validation.** The controller validates inputs first to return descriptive HTTP 400 responses. The service layer (`ImageComposerService`, `LocalStorageService`) also validates its own inputs and throws typed exceptions as defence-in-depth — ensuring correctness regardless of which caller invokes the service.

**Grid columns are fixed; rows grow to fit all images.** No images are ever dropped and no grey placeholder cells are used.

**Resize strategy is letterbox (Pad), not crop.** Each image scales to fit its cell while preserving aspect ratio; white bars fill remaining space.

**Frontend single source of truth for layout config.** `compose.config.js` defines `LAYOUTS`, `COMPOSE_DEFAULTS`, and `getCapacity`. Both the settings form (`ComposeSettings`) and the compose hook (`useCompose`) import from it — changing a default or adding a layout requires updating one file.

**Shared async loading pattern.** The `useAsync` hook encapsulates the `try/catch/finally` loading state pattern used by all three workflow steps. Each step calls `useAsync` rather than managing its own `loading` and `error` state.

**Frontend proxy replaces CORS in development.** Vite proxies `/api` and `/outputs` to port 5000, making requests same-origin from the browser's perspective.

---

## Known Limitations

- Sessions are lost on API process restart (upload files on disk are retained until TTL eviction runs).
- Sessions can only be composed once per upload — upload new images to compose again.
- The in-memory session store does not survive horizontal scaling across multiple instances.
- No authentication or rate limiting — required for any production deployment.
- No secrets or credentials are present in this repository.
