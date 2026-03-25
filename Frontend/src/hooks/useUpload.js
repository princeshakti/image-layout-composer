import { useState, useCallback } from 'react'
import { useAsync } from './useAsync'
import { uploadImages } from '../api/imageApi'

const ACCEPTED = /\.(jpe?g|png)$/i
const MAX_FILES = 16

export function useUpload(onSuccess) {
  const [files, setFiles]             = useState([])
  const [typeWarning, setTypeWarning] = useState('')
  const { run: runUpload, loading, error } = useAsync(
    useCallback(async () => {
      const { sessionId, images } = await uploadImages(files)
      onSuccess(sessionId, images)
    }, [files, onSuccess])
  )

  const addFiles = useCallback((incoming) => {
    const list    = Array.from(incoming)
    const valid   = list.filter((f) => ACCEPTED.test(f.name))
    const skipped = list.length - valid.length

    setTypeWarning(skipped > 0 ? `${skipped} file(s) skipped — only JPG and PNG are accepted.` : '')

    setFiles((prev) => {
      // Deduplication key is name+size. A content hash would be more precise but
      // adds async complexity for a rare edge case.
      const seen   = new Set(prev.map((f) => f.name + f.size))
      const unique = valid.filter((f) => !seen.has(f.name + f.size))
      return [...prev, ...unique].slice(0, MAX_FILES)
    })
  }, [])

  const removeFile = useCallback((index) =>
    setFiles((prev) => prev.filter((_, i) => i !== index)), [])

  return { files, addFiles, removeFile, upload: runUpload, loading, error, typeWarning }
}
