# Image Layout Composer — Frontend

A React + Vite frontend for the Image Layout Composer API.

---

## Prerequisites

- [Node.js 18+](https://nodejs.org/) (LTS recommended)
- The .NET API running on `http://localhost:5000` (see the root README)

---

## Setup & Run

```bash
# 1 — navigate to this folder
cd frontend

# 2 — install dependencies
npm install

# 3 — start the dev server
npm run dev
```

Open **`http://localhost:3000`** in your browser.

The Vite dev server proxies all `/api` and `/outputs` requests to
`http://localhost:5000`, so the API and frontend can run on different ports
without any CORS issues.

---

## Project Structure

```
frontend/
├── index.html                    # Vite HTML entry point
├── vite.config.js                # Dev server + API proxy config
├── package.json
└── src/
    ├── main.jsx                  # React root — mounts <App />
    ├── App.jsx                   # Root component, single state object for step/session/result
    ├── App.module.css            # Header and shell layout styles
    ├── compose.config.js         # Single source of truth: LAYOUTS, COMPOSE_DEFAULTS, getCapacity
    ├── api/
    │   └── imageApi.js           # All fetch calls to the API (upload, compose, download)
    ├── hooks/
    │   ├── useAsync.js           # Shared try/catch/finally loading + error state pattern
    │   ├── useUpload.js          # File selection state + upload API call (uses useAsync)
    │   └── useCompose.js         # Compose settings state + compose API call (uses useAsync)
    ├── components/
    │   ├── StepIndicator.jsx     # 1 / 2 / 3 step pill bar
    │   ├── Alert.jsx             # Error / warning / success banner
    │   ├── Spinner.jsx           # Inline loading spinner
    │   ├── LoadingButton.jsx     # Shared button with spinner + loading label
    │   ├── DropZone.jsx          # Drag-and-drop / click-to-browse file input
    │   ├── FileList.jsx          # Selected files with thumbnails and remove buttons
    │   ├── ComposeSettings.jsx   # Layout, format, cellSize, padding fields (reads LAYOUTS)
    │   ├── ResultStats.jsx       # Stat tiles showing grid dimensions after compose
    │   ├── UploadStep.jsx        # Step 1 page — pick and upload images
    │   ├── ComposeStep.jsx       # Step 2 page — configure and compose grid
    │   └── DownloadStep.jsx      # Step 3 page — preview and download result
    └── styles/
        └── global.css            # CSS variables and base reset
```

---

## Key design decisions

**`compose.config.js` — single source of truth.** `LAYOUTS` (the grid options), `COMPOSE_DEFAULTS` (the initial form values), and `getCapacity` (capacity lookup) all live here. `ComposeSettings.jsx` reads `LAYOUTS` to render the dropdown; `useCompose.js` reads `COMPOSE_DEFAULTS` to initialise state; `ComposeStep.jsx` calls `getCapacity` to compute the extend warning. Adding a new layout or changing a default requires editing one file.

**`useAsync.js` — shared loading state.** All three workflow steps need the same `try/catch/finally` pattern to manage `loading` and `error` state around async API calls. `useAsync(fn)` wraps any async function with that pattern and returns `{ run, loading, error }`. `useUpload`, `useCompose`, and `DownloadStep` all use it rather than duplicating the logic.

**`LoadingButton.jsx` — shared button pattern.** The pattern of showing a `<Spinner>` and a different label while loading appeared identically in all three step components. `LoadingButton` renders this pattern once and accepts `loading`, `loadingLabel`, and `children` as props.

---

## How it connects to the API

All API communication is in `src/api/imageApi.js`:

| Function | API endpoint |
|---|---|
| `uploadImages(files)` | `POST /api/images/upload` |
| `composeGrid(sessionId, options)` | `POST /api/images/{sessionId}/compose` |
| `downloadImage(fileName)` | `GET /api/images/download/{fileName}` |

The Vite proxy in `vite.config.js` forwards these requests to `http://localhost:5000` during development, so no base URL is needed in the fetch calls.

---

## Build for production

```bash
npm run build
```

Output is written to `dist/`. You can serve it with any static file host,
but make sure your deployment points the API base URL to wherever the .NET
API is hosted.
