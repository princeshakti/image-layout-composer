import { useState, useRef } from 'react'
import styles from './DropZone.module.css'

export default function DropZone({ onFiles }) {
  const [dragOver, setDragOver] = useState(false)
  const inputRef = useRef(null)

  const handleDrop = (e) => {
    e.preventDefault()
    setDragOver(false)
    if (e.dataTransfer.files.length) onFiles(e.dataTransfer.files)
  }

  const handleInput = (e) => {
    if (e.target.files.length) onFiles(e.target.files)
    e.target.value = ''
  }

  return (
    <div
      className={`${styles.zone} ${dragOver ? styles.over : ''}`}
      onDrop={handleDrop}
      onDragOver={(e) => { e.preventDefault(); setDragOver(true) }}
      onDragLeave={() => setDragOver(false)}
      onClick={() => inputRef.current?.click()}
    >
      <input ref={inputRef} type="file" multiple accept=".jpg,.jpeg,.png"
        className={styles.input} onChange={handleInput} />
      <div className={styles.icon}>🖼</div>
      <h3 className={styles.title}>Drop images here</h3>
      <p className={styles.hint}>or click to browse — JPG &amp; PNG · up to 16 files · 20 MB each</p>
    </div>
  )
}
