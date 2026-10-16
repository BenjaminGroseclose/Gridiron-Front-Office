// Spike S0b helpers (DevHost only).

/** Height of the first data row inside the grid frame, to compare with the grid's ItemSize. */
export function rowHeight(frameSelector) {
  const frame = document.querySelector(frameSelector);
  if (!frame) return 0;
  const rows = [...frame.querySelectorAll('[role="row"], tr')].filter(r => !r.querySelector('th, [role="columnheader"]'));
  return rows.length ? rows[0].getBoundingClientRect().height : 0;
}

/** Calls back into .NET whenever the OS light/dark preference changes (GfoThemeService owns System mode). */
export function watchColorScheme(dotnet) {
  const query = matchMedia('(prefers-color-scheme: dark)');
  query.addEventListener('change', e => dotnet.invokeMethodAsync('OnSystemSchemeChanged', e.matches));
  window.__spikeScheme = dotnet;   // lets the spike fire the callback by hand (emulation doesn't raise 'change')
  return query.matches;
}
