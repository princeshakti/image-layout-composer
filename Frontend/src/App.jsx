import { useState, useCallback } from 'react'
import StepIndicator from './components/StepIndicator'
import UploadStep    from './components/UploadStep'
import ComposeStep   from './components/ComposeStep'
import DownloadStep  from './components/DownloadStep'
import styles        from './App.module.css'

const INITIAL = { step: 1, sessionId: '', imageCount: 0, result: null }

export default function App() {
  const [state, setState] = useState(INITIAL)

  const handleUploaded = useCallback((sessionId, images) =>
    setState({ step: 2, sessionId, imageCount: images.length, result: null }), [])

  const handleComposed = useCallback((result) =>
    setState(prev => ({ ...prev, step: 3, result })), [])

  const handleBack    = useCallback(() => setState(prev => ({ ...prev, step: 1 })), [])
  const handleReset   = useCallback(() => setState(INITIAL), [])

  const { step, sessionId, imageCount, result } = state

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <p className={styles.wordmark}>Image Layout Composer</p>
        <h1 className={styles.headline}>
          Compose images into <em>perfect grids</em>
        </h1>
        <p className={styles.sub}>
          Upload your photos, pick a layout, and download a clean grid image in seconds.
        </p>
      </header>

      <StepIndicator current={step} />

      {step === 1 && <UploadStep onDone={handleUploaded} />}

      {step === 2 && (
        <ComposeStep
          sessionId={sessionId}
          imageCount={imageCount}
          onDone={handleComposed}
          onBack={handleBack}
        />
      )}

      {step === 3 && result && <DownloadStep result={result} onReset={handleReset} />}
    </div>
  )
}
