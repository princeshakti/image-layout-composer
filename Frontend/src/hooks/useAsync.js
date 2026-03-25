import { useState, useCallback } from 'react'

/**
 * Wraps an async function with shared loading/error state.
 * Eliminates the repeated try/catch/finally pattern across hooks and components.
 *
 * @param {(...args: any[]) => Promise<any>} fn  The async function to wrap.
 * @returns {{ run: Function, loading: boolean, error: string }}
 */
export function useAsync(fn) {
  const [loading, setLoading] = useState(false)
  const [error, setError]     = useState('')

  const run = useCallback(async (...args) => {
    setLoading(true)
    setError('')
    try {
      return await fn(...args)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }, [fn])

  return { run, loading, error }
}
