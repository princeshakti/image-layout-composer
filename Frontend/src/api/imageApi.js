/**
 * imageApi.js
 * All fetch calls to the Image Layout Composer API.
 * The Vite dev server proxies /api and /outputs to http://localhost:5000,
 * so no base URL is needed here.
 */

/**
 * Upload one or more image files.
 * @param {File[]} files
 * @returns {Promise<{ sessionId: string, images: ImageInfo[], message: string }>}
 */
export async function uploadImages(files) {
  const formData = new FormData()
  files.forEach((file) => formData.append('files', file))

  const res = await fetch('/api/images/upload', {
    method: 'POST',
    body: formData,
  })

  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Upload failed')
  return data
}

/**
 * Compose uploaded images into a grid.
 * @param {string} sessionId
 * @param {{ layout: string, format: string, cellSize: number, padding: number }} options
 * @returns {Promise<ComposeResponse>}
 */
export async function composeGrid(sessionId, { layout, format, cellSize, padding }) {
  const params = new URLSearchParams({ layout, format, cellSize, padding })

  const res = await fetch(`/api/images/${sessionId}/compose?${params}`, {
    method: 'POST',
  })

  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Compose failed')
  return data
}

/**
 * Download a composed image as a Blob and trigger a browser save dialog.
 * The anchor is appended to document.body before .click() and removed after —
 * clicking a detached anchor is unreliable in Firefox.
 * @param {string} fileName  The outputFileName from the compose response.
 */
export async function downloadImage(fileName) {
  const res = await fetch(`/api/images/download/${fileName}`)
  if (!res.ok) throw new Error('Download failed')
  const blob = await res.blob()
  const url  = URL.createObjectURL(blob)
  const a    = document.createElement('a')
  a.href            = url
  a.download        = fileName
  a.style.display   = 'none'
  document.body.appendChild(a)
  try {
    a.click()
  } finally {
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }
}

/**
 * @typedef {{ imageId: string, originalFileName: string, storedFileName: string, fileSizeBytes: number, mimeType: string }} ImageInfo
 * @typedef {{ outputFileName: string, downloadUrl: string, layout: string, gridColumns: number, gridRows: number, totalImages: number, placedImages: number, emptyCells: number, warning: string|null }} ComposeResponse
 */
