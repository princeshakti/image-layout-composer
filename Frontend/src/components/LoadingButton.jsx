import Spinner from './Spinner'
import styles  from './Step.module.css'

/**
 * A button that shows a spinner and alternate label while loading.
 * Eliminates the repeated {loading && <Spinner dark />}{loading ? '…' : label} pattern.
 *
 * @param {{
 *   loading: boolean,
 *   loadingLabel?: string,
 *   disabled?: boolean,
 *   onClick: () => void,
 *   className?: string,
 *   style?: object,
 *   children: React.ReactNode,
 * }} props
 */
export default function LoadingButton({
  loading,
  loadingLabel,
  disabled,
  onClick,
  className,
  style,
  children,
}) {
  return (
    <button
      className={className ?? styles.btnPrimary}
      style={style}
      onClick={onClick}
      disabled={disabled ?? loading}
    >
      {loading && <Spinner dark />}
      {loading && loadingLabel ? loadingLabel : children}
    </button>
  )
}
