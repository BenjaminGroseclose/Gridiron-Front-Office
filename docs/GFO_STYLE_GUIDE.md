# Gridiron Front Office | UI Style Guide

## Project Vision

A professional, data-driven football management simulation focused on high-stakes front-office decision-making. The **Executive Suite Dark** palette provides a modern, sophisticated, and authoritative dashboard aesthetic optimized for extended viewing sessions.

---

## Tech Stack

- **CSS Framework:** Tailwind CSS (CDN) with custom theme configuration
- **Component Library:** Custom Blazor components (no third-party UI library)
- **Font:** Inter (Google Fonts) — weights 300–700
- **Icons:** Inline SVGs

---

## 1. Core Brand Colors

These colors define the application's structure and overall "feel." They are configured in the Tailwind theme (`index.html`).

| Element                        | Hex Code  | Tailwind Token  | Usage                                            |
| :----------------------------- | :-------- | :-------------- | :----------------------------------------------- |
| **Primary (Gridiron Blue)**    | `#2563EB` | `primary`       | Sidebars, main headers, and primary branding.    |
| **Primary Light**              | `#3B82F6` | `primary-light` | Hover states on primary elements.                |
| **Primary Dark**               | `#1D4ED8` | `primary-dark`  | Active/pressed states.                           |
| **Secondary (Sideline Slate)** | `#475569` | `secondary`     | Sub-navigation, card headers, secondary buttons. |
| **Background (Abyss)**         | `#0A0E27` | `canvas`        | Main app background; reduced eye strain.         |
| **Surface (Navy)**             | `#1A1F3A` | `surface`       | Table rows, card backgrounds, and input fields.  |
| **Text (Frost)**               | `#F1F5F9` | `frost`         | Primary body text and headings (high contrast).  |
| **Text Secondary**             | `#94A3B8` | `muted`         | Muted secondary text for hierarchy.              |
| **Border (Slate)**             | `#475569` | `divider`       | 1px dividers and subtle structural elements.     |

---

## 2. Status & Functional Colors

Used for real-time feedback, financial health, and player status.

| Status                      | Hex Code  | Tailwind Token | Usage                                                            |
| :-------------------------- | :-------- | :------------- | :--------------------------------------------------------------- |
| **SUCCESS (Stadium Green)** | `#38B000` | `success`      | Contract signings, cap space availability, positive development. |
| **WARNING (Gold)**          | `#FFB703` | `warning`      | Expiring contracts, roster bubble alerts, high fatigue levels.   |
| **ERROR (Flag Red)**        | `#C1121F` | `error`        | Cap violations, failed trades, major injuries, delete actions.   |
| **INFO (Analytics Blue)**   | `#00B4D8` | `info`         | Scouting reports, league news, general tooltips.                 |

---

## 3. Data Visualization

Since the game is simulation-heavy, these rules ensure the UI remains readable.

- **Monospace for Stats:** Use a monospaced font for all tables and financial figures to ensure columns align perfectly.
- **The 90/10 Rule:** Keep 90% of the UI neutral (Navy/Slate/White). Use the Status Colors (Green/Red/Gold) for the remaining 10% to draw focus to critical changes.
- **Borders:** Use `divider` (`#475569`) for 1px dividers and structural elements to maintain hierarchy in dark mode.

---

## 4. Typography

- **Font Family:** Inter (sans-serif)
- **Headings:** Inter Bold (`font-bold`)
- **Body:** Inter Regular (`font-normal`)
- **Numbers:** Monospace (use `font-mono` utility)

---

## 5. Reusable Components

All shared UI components live in `src/UI/Components/Common/`. Use these instead of raw HTML for consistency.

| Component            | Purpose                | Key Parameters                                                              |
| :------------------- | :--------------------- | :-------------------------------------------------------------------------- |
| `AppButton`          | Primary action trigger | `Variant` (filled/outlined/text), `Color`, `Disabled`                       |
| `AppChip`            | Status badges, tags    | `Color`, `Size` (sm/md/lg)                                                  |
| `AppAlert`           | Inline notifications   | `Severity` (error/warning/success/info), `ShowCloseIcon`                    |
| `AppCard`            | Content container      | `Outlined` (bool), with `AppCardHeader`, `AppCardContent`, `AppCardActions` |
| `AppTextField`       | Text input             | `Label`, `Value`, `Placeholder`                                             |
| `AppNumericField<T>` | Numeric input          | `Label`, `Value`, `Min`, `Max`                                              |
| `AppSelect<T>`       | Dropdown select        | `Label`, `Value`, uses `<option>` children                                  |
| `AppCheckbox`        | Toggle boolean         | `Label`, `Value`                                                            |
| `AppAvatar`          | User/team identity     | `Size` (sm/md/lg), `Style`, `Text`                                          |
| `AppTooltip`         | Hover hint             | `Text`, `Placement` (top/bottom/left/right)                                 |
| `AppDialog`          | Modal overlay          | `Title`, `Visible`, `FooterContent`                                         |
| `AppTable<T>`        | Data table             | `Items`, `HeaderContent`, `RowTemplate`, `SelectedItem`                     |
| `AppStepper`         | Multi-step wizard      | `Steps`, `ActiveIndex`, `OnBeforeNext`, `CompletedContent`                  |
| `AppDivider`         | Horizontal rule        | `Class`                                                                     |
| `AppSpinner`         | Loading indicator      | `Size` (sm/md/lg), `Color`                                                  |

---

## 6. Tailwind Usage Guidelines

### Class Ordering Convention

Follow the recommended Tailwind class order: layout → sizing → spacing → typography → colors → effects → states.

```html
<div
    class="flex items-center gap-4 px-4 py-2 text-sm text-frost bg-surface rounded-lg border border-divider hover:bg-primary/10"
></div>
```

### Color Opacity

Use Tailwind's opacity modifier for translucent backgrounds:

- `bg-primary/10` — subtle hover highlight
- `bg-error/15` — alert background
- `bg-success/20` — chip background

### Responsive Design

Use Tailwind's responsive prefixes (`sm:`, `md:`, `lg:`) for grid layouts:

```html
<div class="grid grid-cols-1 sm:grid-cols-2 gap-4"></div>
```

### Dark Theme

The app is dark-only. All components assume a dark canvas. Do not use `dark:` variants.
