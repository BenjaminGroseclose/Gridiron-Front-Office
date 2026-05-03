---
description: "Blazor component and application patterns with Tailwind CSS"
applyTo: "**/*.razor, **/*.razor.cs, **/*.razor.css"
---

## Blazor & Tailwind CSS Code Style and Structure

- Write idiomatic and efficient Blazor and C# code.
- Follow .NET and Blazor conventions.
- Follow tailwind CSS best practices for utility-first styling.
- Use Razor Components appropriately for component-based UI development.
- Prefer inline functions for smaller components but separate complex logic into code-behind or service classes.
- Async/await should be used where applicable to ensure non-blocking UI operations.

## Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

## Blazor and .NET Specific Guidelines

- Utilize Blazor's built-in features for component lifecycle (e.g., OnInitializedAsync, OnParametersSetAsync).
- Use data binding effectively with `@bind`.
- Leverage Dependency Injection for services in Blazor.
- Structure Blazor components and services following Separation of Concerns.
- Always use the latest version C#, currently C# 13 features like record types, pattern matching, and global usings.

## Styling & Colors — CRITICAL

- **NEVER use raw hex codes or RGB values in Razor files.** Always use either:
    - Tailwind utility classes with theme tokens (e.g., `text-primary`, `bg-surface`, `border-divider`)
    - CSS custom properties in `<style>` blocks (e.g., `var(--color-primary)`, `var(--color-frost)`)
- All design tokens are defined in `src/UI/wwwroot/styles/app.css` under `:root`.
- For opacity/transparency, use `color-mix(in srgb, var(--color-primary) 10%, transparent)` in `<style>` blocks, or Tailwind's opacity modifier (`bg-primary/10`) in class attributes.
- Available CSS variable tokens:
    - `--color-primary`, `--color-primary-light`, `--color-primary-dark`, `--color-primary-muted`
    - `--color-secondary`, `--color-secondary-light`, `--color-secondary-dark`
    - `--color-canvas`, `--color-surface`, `--color-surface-elevated`, `--color-surface-hover`
    - `--color-frost`, `--color-muted`, `--color-muted-dark`, `--color-disabled`
    - `--color-divider`, `--color-divider-light`, `--color-ring`
    - `--color-success`, `--color-success-light`, `--color-success-dark`
    - `--color-warning`, `--color-warning-light`, `--color-warning-dark`
    - `--color-error`, `--color-error-light`, `--color-error-dark`
    - `--color-info`, `--color-info-light`, `--color-info-dark`

## Custom Component Usage

- **Layout:** Use Tailwind's `flex`, `grid`, and responsive prefixes (`sm:`, `md:`, `lg:`) for layouts.
- **Icons:** Use inline SVGs. Do not use icon fonts or external icon libraries.
- **Reusable Components:** Use the shared components in `src/UI/Components/Common/`:
    - `AppButton` — buttons (Variant: filled/outlined/text, Color: primary/secondary/error/success/warning/info)
    - `AppChip` — badges/tags
    - `AppAlert` — notifications (Severity: error/warning/success/info)
    - `AppCard`, `AppCardHeader`, `AppCardContent`, `AppCardActions` — content cards
    - `AppTextField` — text inputs
    - `AppNumericField<T>` — numeric inputs
    - `AppSelect<T>` — dropdowns (uses `<option>` children)
    - `AppCheckbox` — toggle inputs
    - `AppAvatar` — identity circles
    - `AppTooltip` — hover hints
    - `AppDialog` — modal overlays
    - `AppTable<T>` — data tables
    - `AppStepper` — multi-step wizards
    - `AppDivider` — horizontal separators
    - `AppSpinner` — loading indicators
- **Dialogs:** Use `AppDialog` with `Visible` binding and `OnClose` callback. Do not use service-based dialog patterns.

## Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls.
- Use logging for error tracking in the backend and consider capturing UI-level errors in Blazor with `ErrorBoundary`.
- Use component-level validation logic (no external validation library required for now).

## Performance Optimization

- Use asynchronous methods (async/await) for API calls or UI actions that could block the main thread.
- Optimize Razor components by reducing unnecessary renders and using `StateHasChanged()` efficiently.
- Minimize the component render tree by avoiding re-renders unless necessary, using `ShouldRender()` where appropriate.

## Caching Strategies

- Implement in-memory caching for frequently used data. Use `IMemoryCache` for lightweight caching solutions.
- For WebView, utilize `localStorage` or `sessionStorage` to cache application state between user sessions.

## State Management

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing.
- Use Scoped Services and the StateContainer pattern (AppState) to manage state within user sessions.

## Testing and Debugging

- Test logic and services using xUnit with Moq/NSubstitute for dependencies.
- Use browser developer tools and Visual Studio's diagnostics for performance profiling.

## Security and Authentication

- Implement Authentication and Authorization using ASP.NET Identity or JWT tokens.
- Use HTTPS for all web communication and ensure proper CORS policies are implemented.
