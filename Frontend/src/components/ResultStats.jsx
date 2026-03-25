import styles from './ResultStats.module.css'

export default function ResultStats({ result }) {
  const { layout, gridColumns, gridRows, placedImages, emptyCells } = result
  const tiles = [
    { label: 'Layout',  value: layout },
    { label: 'Grid',    value: `${gridColumns} × ${gridRows}` },
    { label: 'Placed',  value: placedImages },
    { label: 'Empty',   value: emptyCells },
  ]

  return (
    <dl className={styles.grid}>
      {tiles.map(({ label, value }) => (
        <div key={label} className={styles.tile}>
          <dt>{label}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  )
}
