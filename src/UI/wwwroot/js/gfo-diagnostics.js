// gfo-diagnostics.js: forwards JavaScript errors to the .NET log (LOGGING.md §8).
// Loaded as a classic <script> by the host page BEFORE Blazor, so errors during boot are caught too. They're held
// until the ClientDiagnostics component attaches, then logged as Error with SourceContext GridironFrontOffice.UI.Js.
(() => {
  const limit = 50;          // per session; ClientDiagnostics notes the cut-off
  const pending = [];
  let sink = null;
  let sent = 0;

  const send = entry => {
    if (sent++ >= limit + 1) return;
    sink.invokeMethodAsync('LogClientError', entry.kind, entry.message, entry.source, entry.line, entry.column, entry.stack)
      .catch(() => { /* the circuit or WebView is going away: nothing left to log to */ });
  };

  const report = (kind, message, source, line, column, stack) => {
    const entry = {
      kind,
      message: String(message ?? ''),
      source: String(source ?? ''),
      line: Number(line) || 0,
      column: Number(column) || 0,
      stack: String(stack ?? ''),
    };
    if (sink) send(entry);
    else if (pending.length < limit) pending.push(entry);
  };

  // Script errors only: resource load failures don't bubble to window.
  window.addEventListener('error', e => report('error', e.message, e.filename, e.lineno, e.colno, e.error?.stack));
  window.addEventListener('unhandledrejection', e => {
    const reason = e.reason;
    report('unhandled rejection', reason?.message ?? reason, '', 0, 0, reason?.stack);
  });

  window.gfoDiagnostics = {
    attach(dotnet) {
      sink = dotnet;
      pending.splice(0).forEach(send);
    },
    detach() {
      sink = null;
    },
    /** Debug-only test action: an uncaught error outside any .NET call. */
    throwTestError() {
      setTimeout(() => { throw new Error('Test JavaScript error from the diagnostics panel'); }, 0);
    },
  };
})();
