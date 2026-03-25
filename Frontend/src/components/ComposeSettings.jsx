import { LAYOUTS } from '../compose.config'
import styles from './ComposeSettings.module.css'

export default function ComposeSettings({ settings, onChange }) {
  return (
    <div className={styles.grid}>
      <div className={styles.field}>
        <label htmlFor="layout">Layout</label>
        <select id="layout" value={settings.layout} onChange={(e) => onChange('layout', e.target.value)}>
          {LAYOUTS.map(({ value, label }) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
      </div>

      <div className={styles.field}>
        <label htmlFor="format">Output format</label>
        <select id="format" value={settings.format} onChange={(e) => onChange('format', e.target.value)}>
          <option value="Png">PNG</option>
          <option value="Jpeg">JPEG</option>
        </select>
      </div>

      <div className={styles.field}>
        <label htmlFor="cellSize">Cell size (px)</label>
        <input id="cellSize" type="number" min={50} max={2000}
          value={settings.cellSize} onChange={(e) => onChange('cellSize', Number(e.target.value))} />
      </div>

      <div className={styles.field}>
        <label htmlFor="padding">Padding (px)</label>
        <input id="padding" type="number" min={0} max={100}
          value={settings.padding} onChange={(e) => onChange('padding', Number(e.target.value))} />
      </div>
    </div>
  )
}
