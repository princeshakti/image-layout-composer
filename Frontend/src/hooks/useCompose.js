import { useState, useCallback } from 'react'
import { useAsync } from './useAsync'
import { composeGrid } from '../api/imageApi'
import { COMPOSE_DEFAULTS } from '../compose.config'

export function useCompose(sessionId, onSuccess) {
  const [settings, setSettings] = useState(COMPOSE_DEFAULTS)

  const { run: runCompose, loading, error } = useAsync(
    useCallback(async () => {
      const result = await composeGrid(sessionId, settings)
      onSuccess(result)
    }, [sessionId, settings, onSuccess])
  )

  const updateSetting = useCallback((key, value) =>
    setSettings((prev) => ({ ...prev, [key]: value })), [])

  return { settings, updateSetting, compose: runCompose, loading, error }
}
