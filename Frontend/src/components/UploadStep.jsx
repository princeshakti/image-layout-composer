import { useUpload }    from '../hooks/useUpload'
import DropZone         from './DropZone'
import FileList         from './FileList'
import Alert            from './Alert'
import LoadingButton    from './LoadingButton'
import styles           from './Step.module.css'

export default function UploadStep({ onDone }) {
  const { files, addFiles, removeFile, upload, loading, error, typeWarning } = useUpload(onDone)

  const label = files.length
    ? `Upload ${files.length} image${files.length !== 1 ? 's' : ''}`
    : 'Upload images'

  return (
    <div className={`${styles.step} fade-up`}>
      <div className={styles.card}>
        <p className={styles.cardTitle}>Select images</p>
        <DropZone onFiles={addFiles} />
        <FileList files={files} onRemove={removeFile} />
      </div>

      {typeWarning && <Alert type="warn"  message={typeWarning} />}
      {error       && <Alert type="error" message={error} />}

      <LoadingButton
        loading={loading}
        loadingLabel="Uploading…"
        disabled={!files.length || loading}
        onClick={upload}
      >
        {label}
      </LoadingButton>
    </div>
  )
}
