// Spike S0c helpers (Desktop host only).

/** Ends the boot hide once the theme is in place (index.html: html.gfo-booting #app { visibility: hidden }). */
export function reveal() {
  document.documentElement.classList.remove('gfo-booting');
}

const holeSelector = 'button, input, a, select, textarea, fluent-button, fluent-text-input, [data-no-drag]';
const rect = r => [r.left, r.top, r.width, r.height];

/**
 * Watches prefers-color-scheme (GFO owns Match Windows, D-067) and, for title bar option (b), reports the top bar's
 * rectangle and its interactive holes to .NET whenever the layout changes, so WM_NCHITTEST can answer HTCAPTION.
 */
export function watch(dotnet, reportDragRegions) {
  const query = matchMedia('(prefers-color-scheme: dark)');
  query.addEventListener('change', e => dotnet.invokeMethodAsync('OnSchemeChanged', e.matches));

  if (reportDragRegions) {
    const bar = document.querySelector('[data-drag-region]');
    const report = () => dotnet.invokeMethodAsync('SetDragRegions',
      rect(bar.getBoundingClientRect()),
      [...bar.querySelectorAll(holeSelector)].map(e => rect(e.getBoundingClientRect())),
      devicePixelRatio);
    new ResizeObserver(report).observe(bar);
    report();
  }

  return query.matches;
}
