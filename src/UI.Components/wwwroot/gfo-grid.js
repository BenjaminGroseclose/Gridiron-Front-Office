// DataGrid<T>'s keyboard additions (UI_DESIGN_SYSTEM.md §5.3). Fluent moves focus between cells with the arrow keys; this
// adds PageUp/PageDown (a frame's worth of rows), Home/End (first and last cell of the row) and Ctrl+Home/End (first and
// last row), and hands Enter (open) and Space (select) to .NET. Fluent's cells stop key events inside Blazor's event
// dispatch, so a Blazor @onkeydown on the frame never sees them; a DOM listener on the frame does. Fluent's rows are
// display: contents (no box of their own), so heights are measured on cells.

const cellSelector = 'td, th';

export function attach(frame, dotnet) {
  const onKeyDown = e => {
    const cell = e.target.closest(cellSelector);
    if (!cell || !frame.contains(cell) || e.altKey || e.metaKey) return;
    const body = cell.tagName === 'TD';

    if (body && !e.ctrlKey && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault();   // Space would scroll the frame
      dotnet.invokeMethodAsync('OnKey', e.key);
      return;
    }

    const row = cell.closest('tr');
    const cells = [...row.querySelectorAll(cellSelector)];
    const column = cells.indexOf(cell);

    if (!e.ctrlKey && (e.key === 'Home' || e.key === 'End')) {
      e.preventDefault();
      (e.key === 'Home' ? cells[0] : cells[cells.length - 1]).focus();
      return;
    }

    if (!body) return;
    const index = Number(row.getAttribute('aria-rowindex'));
    const last = Number(frame.querySelector('table')?.getAttribute('aria-rowcount')) || index;
    const header = frame.querySelector('thead th')?.getBoundingClientRect().height ?? 0;
    const rowHeight = cell.getBoundingClientRect().height || 1;
    const page = Math.max(1, Math.floor((frame.clientHeight - header) / rowHeight) - 1);
    const first = 2;   // row 1 is the header

    let target;
    if (e.key === 'PageDown') target = Math.min(last, index + page);
    else if (e.key === 'PageUp') target = Math.max(first, index - page);
    else if (e.ctrlKey && e.key === 'Home') target = first;
    else if (e.ctrlKey && e.key === 'End') target = last;
    else return;

    e.preventDefault();
    if (!index || target === index) return;

    // The target row may not be rendered yet (virtualization): scroll it into view, then focus it once it exists.
    const rowTop = header + (target - first) * rowHeight;
    if (rowTop < frame.scrollTop + header) frame.scrollTop = rowTop - header;
    else if (rowTop + rowHeight > frame.scrollTop + frame.clientHeight) frame.scrollTop = rowTop + rowHeight - frame.clientHeight;
    focusWhenRendered(frame, target, column);
  };

  frame.addEventListener('keydown', onKeyDown);
  return { dispose: () => frame.removeEventListener('keydown', onKeyDown) };
}

function focusWhenRendered(frame, rowIndex, column, attempt = 0) {
  const row = frame.querySelector(`tr[aria-rowindex="${rowIndex}"]`);
  const cells = row ? [...row.querySelectorAll(cellSelector)] : [];
  if (cells.length) {
    (cells[column] ?? cells[0]).focus({ preventScroll: true });
  } else if (attempt < 60) {
    requestAnimationFrame(() => focusWhenRendered(frame, rowIndex, column, attempt + 1));
  }
}
