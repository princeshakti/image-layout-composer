import styles from './Spinner.module.css'

/**
 * Small inline loading spinner.
 * @param {{ dark?: boolean }} props  dark=true uses a dark-coloured border (for use on light backgrounds)
 */
export default function Spinner({ dark = false }) {
  return <span className={`${styles.spinner} ${dark ? styles.dark : ''}`} />
}
