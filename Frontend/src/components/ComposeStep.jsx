import { useCompose }    from '../hooks/useCompose'
import { getCapacity }   from '../compose.config'
import ComposeSettings   from './ComposeSettings'
import Alert             from './Alert'
import LoadingButton     from './LoadingButton'
import styles            from './Step.module.css'

export default function ComposeStep({ sessionId, imageCount, onDone, onBack }) {
  const { settings, updateSetting, compose, loading, error } = useCompose(sessionId, onDone)

  const capacity     = getCapacity(settings.layout)
  const extendWarning = imageCount > capacity
    ? `${imageCount} images exceed the ${capacity}-cell ${settings.layout.replace('Grid', '')} grid. Extra rows will be added automatically.`
    : ''

  return (
    <div className={`${styles.step} fade-up`}>
      <div className={styles.sessionBadge}>
        <span className={styles.sessionLabel}>Session</span>
        <span className={styles.sessionId}>{sessionId.slice(0, 16)}…</span>
        <span className={styles.sessionCount}>· {imageCount} image{imageCount !== 1 ? 's' : ''}</span>
      </div>

      <div className={styles.card}>
        <p className={styles.cardTitle}>Grid settings</p>
        <ComposeSettings settings={settings} onChange={updateSetting} />
        {extendWarning && <Alert type="warn"  message={extendWarning} />}
        {error         && <Alert type="error" message={error} />}
      </div>

      <div className={styles.row}>
        <button className={styles.btnOutline} onClick={onBack} disabled={loading}>← Back</button>
        <LoadingButton
          loading={loading}
          loadingLabel="Composing…"
          onClick={compose}
          style={{ flex: 1 }}
        >
          Compose grid
        </LoadingButton>
      </div>
    </div>
  )
}
