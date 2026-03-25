import styles from './StepIndicator.module.css'

const STEPS = ['Upload', 'Compose', 'Download']

export default function StepIndicator({ current }) {
  return (
    <div className={styles.steps}>
      {STEPS.map((label, i) => {
        const num = i + 1
        const cls = [styles.pill, num < current && styles.done, num === current && styles.active]
          .filter(Boolean).join(' ')
        return (
          <div key={label} className={cls}>
            <span className={styles.num}>{num < current ? '✓' : num}</span>
            {label}
          </div>
        )
      })}
    </div>
  )
}
