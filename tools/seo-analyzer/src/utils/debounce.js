/**
 * Lightweight debounce (450ms default). No lodash.
 * @param {Function} fn
 * @param {number} wait
 */
export function debounce(fn, wait = 450) {
  let timer = null;
  function debounced(...args) {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      timer = null;
      fn.apply(this, args);
    }, wait);
  }
  debounced.cancel = () => {
    if (timer) {
      clearTimeout(timer);
      timer = null;
    }
  };
  debounced.flush = (...args) => {
    debounced.cancel();
    fn.apply(this, args);
  };
  return debounced;
}
