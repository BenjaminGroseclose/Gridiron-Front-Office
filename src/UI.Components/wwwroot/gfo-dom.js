// Small DOM helpers for GFO components that wrap Fluent's web components, whose own element references aren't usable.

/** Focuses the checked option inside a group (SegmentedControl, after the keyboard moved the choice). */
export function focusChecked(group) {
  group?.querySelector('[aria-checked="true"]')?.focus();
}
