// GfoThemeService's page side (UI_DESIGN_SYSTEM.md §3.2–3.3).

let watch = null;

/** Watches the Windows light/dark setting and reports every change. Returns whether Windows is dark now. */
export function start(dotnet) {
  stop();
  const query = matchMedia('(prefers-color-scheme: dark)');
  const changed = e => dotnet.invokeMethodAsync('OnWindowsThemeChanged', e.matches);
  query.addEventListener('change', changed);
  watch = { query, changed };
  return query.matches;
}

export function stop() {
  if (watch) {
    watch.query.removeEventListener('change', watch.changed);
    watch = null;
  }
}

/** After a theme is applied: the attributes gfo-tokens.css reads, then the end of the boot hide (index.html). */
export function applied(mode, density) {
  const root = document.documentElement;
  root.dataset.theme = mode;
  root.dataset.density = density;
  root.classList.remove('gfo-booting');
}
