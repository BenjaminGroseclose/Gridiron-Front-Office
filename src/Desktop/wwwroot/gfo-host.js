// The desktop host's side of the title bar (D-068, UI_DESIGN_SYSTEM.md §7). Only the desktop host page loads it.
// It tells the window two things through WebView2's message channel:
//  - the drag regions: each [data-drag-region] element's rectangle and its interactive holes, in CSS px relative to
//    the web view (equal to DIPs, because page zoom is off), whenever the layout may have changed;
//  - the theme mode, whenever GfoThemeService sets data-theme on the root element.
// The messages are JSON strings with a "gfo" kind. BlazorWebView shares the channel and ignores strings without its
// own prefix.
(() => {
  const webview = window.chrome && window.chrome.webview;
  if (!webview) {
    return;
  }

  // What can't be a drag area: anything interactive, plus [data-no-drag] for custom controls.
  const holes = [
    'a[href]', 'button', 'input', 'select', 'textarea', '[tabindex]:not([tabindex="-1"])', '[data-no-drag]',
    '[role=button]', '[role=link]', '[role=combobox]', '[role=searchbox]', '[role=tab]', '[role=menuitem]',
    'fluent-button', 'fluent-toggle-button', 'fluent-menu-button', 'fluent-text-input', 'fluent-dropdown',
    'fluent-combobox', 'fluent-link', 'fluent-switch',
  ].join(',');
  const rect = element => {
    const r = element.getBoundingClientRect();
    return [r.left, r.top, r.width, r.height];
  };
  const visible = r => r[2] > 0 && r[3] > 0;

  let lastRegions = '';
  let pending = false;
  const reportDragRegions = () => {
    pending = false;
    const regions = [...document.querySelectorAll('[data-drag-region]')]
      .map(bar => ({ bar: rect(bar), holes: [...bar.querySelectorAll(holes)].map(rect).filter(visible) }))
      .filter(region => visible(region.bar));
    const message = JSON.stringify({ gfo: 'drag', regions });
    if (message !== lastRegions) {
      lastRegions = message;
      webview.postMessage(message);
    }
  };
  const schedule = () => {
    if (!pending) {
      pending = true;
      requestAnimationFrame(reportDragRegions);
    }
  };

  // Any DOM change can move a bar or a hole: navigation, a new button, text that changes width. Measured at most once
  // per frame, and sent only when something actually moved.
  new MutationObserver(schedule).observe(document.documentElement, { subtree: true, childList: true, attributes: true, characterData: true });
  addEventListener('resize', schedule);
  document.addEventListener('transitionend', schedule, true);
  document.fonts.addEventListener('loadingdone', schedule);

  // data-theme appears once the theme service has applied, and changes with every mode switch.
  let mode = null;
  new MutationObserver(() => {
    const now = document.documentElement.dataset.theme;
    if (now && now !== mode) {
      mode = now;
      webview.postMessage(JSON.stringify({ gfo: 'theme', mode }));
    }
  }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
})();
