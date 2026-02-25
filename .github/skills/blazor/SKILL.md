---
description: 'Blazor component and application patterns with MudBlazor integration'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

## Blazor & MudBlazor Code Style and Structure

- Write idiomatic and efficient Blazor and C# code.
- Follow .NET, Blazor, and **MudBlazor** library conventions.
- Use Razor Components appropriately for component-based UI development.
- **MudBlazor Pattern:** Use the backing `ParameterState` (`.Value` / `.SetValueAsync`) for property updates rather than overwriting parameters directly to ensure lifecycle consistency.
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

## MudBlazor Component Usage

- **Layout:** Always use `<MudGrid>` and `<MudItem>` for responsive layouts instead of raw HTML/CSS grids. Utilize `xs`, `sm`, `md`, `lg` attributes for breakpoint control.
- **Icons:** Use the static classes `Icons.Material.Filled`, `Icons.Material.Outlined`, or `Icons.Material.Rounded` for the `Icon` parameter.
- **Styling:** Use `MudBlazor.Utilities.CssBuilder` for dynamic classes. Prefer MudBlazor CSS variables (e.g., `var(--mud-palette-primary)`) over hard-coded HEX/RGB values to maintain theme consistency.
- **Dialogs & SnackBar:** Inject `IDialogService` and `ISnackbar`. Invoke dialogs using `DialogService.ShowAsync<T>()` and notifications via `Snackbar.Add()`.
- **References:** Never set component parameters via `@ref` (violates BL0005). Use declarative data binding.

## Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls.
- Use logging for error tracking in the backend and consider capturing UI-level errors in Blazor with tools like `ErrorBoundary`.
- **MudBlazor Validation:** Prefer `<MudForm>` integrated with **FluentValidation**. Use `@bind-Value` on input components to ensure validation state is tracked correctly.

## Blazor API and Performance Optimization

- Utilize Blazor server-side or WebAssembly optimally based on the project requirements.
- **MudTable Optimization:** For large datasets, always use the `ServerData` property in `<MudTable>` to handle pagination, filtering, and sorting on the backend rather than loading all items into memory.
- Use asynchronous methods (async/await) for API calls or UI actions that could block the main thread.
- Optimize Razor components by reducing unnecessary renders and using `StateHasChanged()` efficiently.
- Minimize the component render tree by avoiding re-renders unless necessary, using `ShouldRender()` where appropriate.

## Caching Strategies

- Implement in-memory caching for frequently used data, especially for Blazor Server apps. Use `IMemoryCache` for lightweight caching solutions.
- For Blazor WebAssembly, utilize `localStorage` or `sessionStorage` to cache application state between user sessions.
- Consider Distributed Cache strategies (like Redis) for larger applications.

## State Management Libraries

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing.
- Implement advanced state management solutions using libraries like **Fluxor** when complexity grows.
- For server-side Blazor, use Scoped Services and the StateContainer pattern to manage state within user sessions.

## API Design and Integration

- Use `HttpClient` or other appropriate services to communicate with external APIs.
- Implement error handling for API calls using try-catch and provide proper user feedback in the UI using `MudSnackbar` or `MudMessageBox`.

## Testing and Debugging

- All unit testing and integration testing should be done in Visual Studio Enterprise.
- **Component Testing:** Use **bUnit** for testing MudBlazor components.
- Test logic and services using xUnit, NUnit, or MSTest with Moq/NSubstitute for dependencies.
- Use browser developer tools and Visual Studio's diagnostics for performance profiling.

## Security and Authentication

- Implement Authentication and Authorization using ASP.NET Identity or JWT tokens.
- Use HTTPS for all web communication and ensure proper CORS policies are implemented.

## API Documentation and Swagger

- Use Swagger/OpenAPI for API documentation for backend services.
- Ensure XML documentation for models and API methods to enhance Swagger output.