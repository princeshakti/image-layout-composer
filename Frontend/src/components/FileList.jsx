import { useState, useEffect } from 'react'
import styles from './FileList.module.css'

function formatBytes(bytes) {
  if (bytes < 1024)           return `${bytes} B`
  if (bytes < 1024 * 1024)   return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

/**
 * Renders a list of selected File objects with thumbnail, name, size, and remove button.
 *
 * @param {{
 *   files: File[],
 *   onRemove: (index: number) => void
 * }} props
 */
export default function FileList({ files, onRemove }) {
  if (!files.length) return null

  return (
    <ul className={styles.list}>
      {files.map((file, i) => (
        <FileRow key={file.name + file.size + i} file={file} index={i} onRemove={onRemove} />
      ))}
    </ul>
  )
}

function FileRow({ file, index, onRemove }) {
  const [previewUrl, setPreviewUrl] = useState('')

  // useEffect guarantees the object URL is revoked when the component unmounts
  // or when the file changes — even if the <img> never fires onLoad.
  // The old useMemo approach only revoked on successful image load, leaking the
  // URL if the image failed to render or the row was removed before load fired.
  useEffect(() => {
    const url = URL.createObjectURL(file)
    setPreviewUrl(url)
    return () => URL.revokeObjectURL(url)
  }, [file])

  return (
    <li className={styles.row}>
      <img
        className={styles.thumb}
        src={previewUrl}
        alt={file.name}
      />
      <span className={styles.name}>{file.name}</span>
      <span className={styles.size}>{formatBytes(file.size)}</span>
      <button
        className={styles.remove}
        onClick={() => onRemove(index)}
        aria-label={`Remove ${file.name}`}
      >
        ×
      </button>
    </li>
  )
}
