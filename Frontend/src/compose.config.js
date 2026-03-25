/**
 * compose.config.js
 * Single source of truth for layout definitions and compose defaults.
 * Used by both ComposeSettings (UI) and useCompose (state/API).
 */

export const LAYOUTS = [
  { value: 'Grid2x2', label: '2 × 2 grid', capacity: 4  },
  { value: 'Grid3x3', label: '3 × 3 grid', capacity: 9  },
  { value: 'Grid4x4', label: '4 × 4 grid', capacity: 16 },
]

export const COMPOSE_DEFAULTS = {
  layout:   'Grid2x2',
  format:   'Png',
  cellSize: 400,
  padding:  10,
}

/** Returns the square cell capacity for the given layout value. */
export function getCapacity(layout) {
  return LAYOUTS.find((l) => l.value === layout)?.capacity ?? 4
}
