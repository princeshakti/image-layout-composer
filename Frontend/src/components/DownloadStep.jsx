import { useCallback }  from 'react'
import { useAsync }     from '../hooks/useAsync'
import { downloadImage } from '../api/imageApi'
import ResultStats      from './ResultStats'
import Alert            from './Alert'
import LoadingButton    from './LoadingButton'
import styles           from './Step.module.css'

export default function DownloadStep({ result, onReset }) {
  const { run: download, loading, error } = useAsync(
    useCallback(() => downloadImage(result.outputFileName), [result.outputFileName])
  )

  return (
    <div className={`${styles.step} fade-up`}>
      {result.warning && <Alert type="warn" message={result.warning} />}

      <div className={styles.imageWrap}>
        <img src={result.downloadUrl} alt="Composed grid" className={styles.resultImage} />
      </div>

      <ResultStats result={result} />

      {error && <Alert type="error" message={error} />}

      <LoadingButton loading={loading} loadingLabel="Saving…" onClick={download}>
        ↓  Download image
      </LoadingButton>

      <div className={styles.resetRow}>
        <button className={styles.btnGhost} onClick={onReset}>Start over</button>
      </div>
    </div>
  )
}
