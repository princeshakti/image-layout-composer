import styles from './Alert.module.css'

const ICONS = { error: '✕', warn: '!', success: '✓' }

/**
 * Inline alert banner.
 * @param {{ type: 'error' | 'warn' | 'success', message: string }} props
 */
export default function Alert({ type, message }) {
  if (!message) return null
  return (
    <div className={`${styles.alert} ${styles[type]} fade-up`}>
      <span className={styles.icon}>{ICONS[type]}</span>
      <span>{message}</span>
    </div>
  )
}
