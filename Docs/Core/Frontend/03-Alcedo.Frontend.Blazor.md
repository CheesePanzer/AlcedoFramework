# 03 — Alcedo.Frontend.Blazor

Alcedo.Frontend.Blazor is the shared UI infrastructure layer. It provides layouts, navigation, themes, request execution, the Auto component system (reflection-driven form/table/search rendering), MasterData page bases, and reusable shared components. It depends on `Alcedo.Frontend.Core` and `Alcedo.Frontend.Abstraction`, and uses MudBlazor 9.x as the component library.

---

## 1. Startup Configuration

### 1.1 `AddAlcedoFrontEndBlazor`

```csharp
public static WebAssemblyHostBuilder AddAlcedoFrontEndBlazor(
    this WebAssemblyHostBuilder builder,
    AlcedoModulesRegistry registry,
    Action<AlcedoFrontendBlazorOptions>? configure = null);
```

Registers:

| Service | Lifetime | Purpose |
|---|---|---|
| `AlcedoFrontendBlazorOptions` | Singleton | App display name |
| `ThemeStateProvider` | Singleton | Dark/light mode toggle |
| `GlobalOverlayService` | Singleton | Global loading overlay state |
| `NavRegistry` | Singleton | Navigation menu items (auto-discovered) |
| `BarRegistry` | Singleton | Top app bar items (auto-discovered) |
| `AutoValidatorService` | Scoped | Validation with MudBlazor dialog integration |
| `AlcedoRequestService` | Scoped | Unified request execution pipeline |
| MudBlazor services | — | `AddMudServices()` |

### 1.2 `AlcedoFrontendBlazorOptions`

```csharp
public sealed class AlcedoFrontendBlazorOptions
{
    public string AppDisplayName { get; set; } = "Alcedo Atthis";
}
```

Consumed by `MainLayout` (app bar title) and `NavMenu` (drawer header).

### 1.3 `MudBlazorProvider`

Every Blazor app must wrap its root content in this component:

```razor
@inject ThemeStateProvider ThemeState

<MudThemeProvider IsDarkMode="@_isDarkMode" Theme="@_theme"/>
<GlobalOverlay/>
<MudPopoverProvider/>
<MudDialogProvider/>
<MudSnackbarProvider/>

@ChildContent
```

It initializes the MudBlazor theme, listens for theme changes (dark/light), and renders the overlay/dialog/snackbar providers.

---

## 2. Navigation System

### 2.1 `NavItem`

```csharp
public sealed class NavItem
{
    public required string Title { get; set; }
    public string? Href { get; set; }
    public required string Icon { get; set; }       // MudBlazor icon string
    public List<NavItem> Children { get; set; } = [];
    public bool HasChildren => Children.Count > 0;
}
```

Tree structure: leaf nodes have `Href` set, group nodes have `Children`.

### 2.2 `NavRegistry`

```csharp
public class NavRegistry
{
    public IReadOnlyList<NavItem> Items { get; }
    public void Register(NavItem item);
    public void Register(params NavItem[] items);
}
```

Items are auto-discovered at startup by scanning module assemblies for `INavContributor` implementations.

### 2.3 `INavContributor`

```csharp
public interface INavContributor
{
    IEnumerable<NavItem> GetItems();
}
```

Each module provides its nav items by implementing this in its assembly:

```csharp
// Inside your business module assembly:
public class MyModuleNavContributor : INavContributor
{
    public IEnumerable<NavItem> GetItems() =>
    [
        new()
        {
            Title = "Products",
            Icon = Icons.Material.Outlined.Inventory2,
            Children =
            [
                new() { Title = "All Products", Icon = Icons.Material.Outlined.List, Href = "/products" },
                new() { Title = "New Product",  Icon = Icons.Material.Outlined.Add,   Href = "/products/new" },
            ]
        }
    ];
}
```

No manual registration needed — the assembly scan in `AddNavItem` finds it.

### 2.4 `NavMenu` and `NavItemRenderer`

`NavMenu` iterates `NavRegistry.Items` and delegates each to `NavItemRenderer`:

```razor
<MudNavMenu>
    @foreach (var item in NavRegistry.Items)
    {
        <NavItemRenderer Item="@item"/>
    }
</MudNavMenu>
```

`NavItemRenderer` renders recursively: groups become `MudNavGroup`, leaves become `MudNavLink`.

---

## 3. Bar System (Top App Bar Icons)

### 3.1 `IBarItem`

```csharp
public interface IBarItem
{
    public string Title { get; set; }
    public string? Href { get; set; }
    public string? Icon { get; set; }
}
```

### 3.2 `BarRegistry`

```csharp
public class BarRegistry
{
    public IReadOnlyList<IBarItem> Groups { get; }
    public void Register(IBarItem group);
}
```

Auto-discovered from assemblies via `IBarItem` implementations. Rendered as `MudIconButton` in `MainLayout`'s app bar.

### 3.3 Profile Bar Item Example

```csharp
// In Identity module assembly:
public class ProfileBarItem : IBarItem
{
    public string Title { get; set; } = "Profile";
    public string? Href { get; set; } = "/identity/profile";
    public string? Icon { get; set; } = Icons.Material.Filled.AccountCircle;
}
```

---

## 4. Layouts

### 4.1 `MainLayout`

```
┌──────────────────────────────────────────────────────────────┐
│ AppBar [☰] <AppName>                [BarItems] [🌙/☀️]      │
├─────────────┬────────────────────────────────────────────────┤
│ Drawer      │                                                │
│             │                                                │
│ <NavMenu/>  │           <Body/>                              │
│             │                                                │
└─────────────┴────────────────────────────────────────────────┘
```

Features:
- `MudAppBar` with drawer toggle, app name link, bar items, dark mode toggle
- `MudDrawer` containing `NavMenu`
- `MudMainContent` wrapping `@Body` in a full-width container
- Dark mode persists via `localStorage.getItem("dark_mode")`

### 4.2 `EmptyLayout`

```razor
@inherits LayoutComponentBase
@Body
```

No chrome. Used by `Login.razor` via `@layout EmptyLayout`.

---

## 5. Theme (`ThemeStateProvider`)

```csharp
public class ThemeStateProvider(IJSRuntime JS)
{
    public event Func<Task>? OnThemeChanged;

    public async Task<bool> IsDarkMode();
    public async Task SetDarkMode();
    public async Task UnsetDarkMode();
}
```

Reads/writes `dark_mode` in `localStorage`. `MudBlazorProvider` subscribes to `OnThemeChanged` and calls `StateHasChanged` to update the theme. The theme toggle button in `MainLayout` calls `SetDarkMode`/`UnsetDarkMode`.

---

## 6. Global Overlay

### 6.1 `GlobalOverlayService`

```csharp
public class GlobalOverlayService
{
    public bool IsVisible { get; }
    public event Action? OnChanged;

    public void Show(bool darkBackground = true, bool autoClose = false, string? overlayText = null);
    public void Hide();
}
```

Singleton. Controls visibility of a full-screen loading overlay. `AlcedoRequestService` calls `Show()`/`Hide()` around operations.

### 6.2 `GlobalOverlay` Component

Renders a `MudOverlay` with a `MudProgressCircular` spinner and text. Subscribes to `GlobalOverlayService.OnChanged` via `InvokeAsync(StateHasChanged)`. Rendered inside `MudBlazorProvider`.

---

## 7. `AlcedoRequestService`

The single most-used service by business pages. Provides a unified pipeline for all API calls.

### 7.1 Signature Overloads

| Overload | Use Case |
|---|---|
| `Task ExecuteAsync(Func<Task>, opts)` | Fire-and-forget (POST/PUT/DELETE with no return) |
| `Task<T?> ExecuteAsync<T>(Func<Task<T>>, opts)` | Single API call with return value |
| `Task ExecuteAsync(IReadOnlyCollection<Func<Task>>, opts)` | Batch fire-and-forget (N operations, collects all errors) |
| `Task<IReadOnlyCollection<T?>> ExecuteAsync<T>(IReadOnlyCollection<Func<Task<T>>>, opts)` | Batch calls with return values |

### 7.2 Execution Pipeline

```
1. Confirmation dialog  (if RequestOptions.ConfirmMessage is set)
2. Show overlay          (if RequestOptions.UseOverlay is true)
3. Acquire SemaphoreSlim (1,1) — prevents concurrent requests
4. Execute action
5. Catch ApiBusinessException → show error dialog
6. Release SemaphoreSlim
7. Hide overlay
8. Show success snackbar (if RequestOptions.SuccessMessage is set)
9. Execute OnSuccess / OnFailure callback
```

The global semaphore (static `SemaphoreSlim(1,1)`) ensures only one API call runs at a time — preventing race conditions in the UI.

### 7.3 `RequestOptions` (Fluent API)

```csharp
new RequestOptions()
    .WithConfirmation("Archive 5 items?", "Confirm")
    .WithSuccessMsg("Archived successfully")
    .ExecuteOnSuccess(async () => await RefreshData())
    .ExecuteOnFailure(async () => await HandleFailure())
    .DisableOverlay()
```

| Method | Effect |
|---|---|
| `WithConfirmation(message, title)` | Show MudBlazor message box before executing |
| `WithSuccessMsg(message)` | Show green snackbar on success |
| `ExecuteOnSuccess(Func<Task>)` | Async callback after success |
| `ExecuteOnSuccess(Action)` | Sync callback after success |
| `ExecuteOnFailure(Func<Task>)` | Async callback after failure |
| `ExecuteOnFailure(Action)` | Sync callback after failure |
| `DisableOverlay()` | Skip the loading overlay for this request |

### 7.4 Usage Example

```razor
@inject AlcedoRequestService RequestService

@code {
    private async Task Save()
    {
        await RequestService.ExecuteAsync(
            () => Client.UpdateAsync(id, command),
            opt => opt.WithSuccessMsg("Saved")
                      .ExecuteOnSuccess(() => RefreshData()));
    }

    private async Task BulkDelete(IReadOnlyCollection<Item> items)
    {
        await RequestService.ExecuteAsync(
            items.Select(i => (Func<Task>)(() => Client.DeleteAsync(i.Id))).ToList(),
            opt => opt.WithConfirmation($"Delete {items.Count} item(s)?")
                      .WithSuccessMsg());
        await RefreshData();
    }
}
```

---

## 8. Auto Component System

The Auto component system renders forms, tables, and search panels from DTO/Command types via reflection. Properties are discovered in type-hierarchy order (base classes first, derived classes last) by `AutoComponentOrderedProperties`.

### 8.1 `AutoComponentOrderedProperties`

```csharp
public static class AutoComponentOrderedProperties
{
    public static IEnumerable<PropertyInfo> Get(Type type)
    {
        // Walks type hierarchy from object → ... → most-derived
        // Collects DeclaredOnly properties at each level
        // Base class properties render before subclass properties
    }
}
```

### 8.2 `AutoTable<TItem>`

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `Items` | `IReadOnlyCollection<TItem>` | Data rows |
| `Selectable` | `bool` | Enable checkboxes |
| `SelectedItemsChanged` | `EventCallback<IReadOnlyList<TItem>>` | Selection change handler |
| `OnRowClick` | `EventCallback<TItem>` | Row click handler |
| `SelectOnRowClick` | `bool` (default: `true`) | Select on row click |
| `Comparer` | `IEqualityComparer<TItem>` | Reference equality by default |

**Column rendering rules:**

| Property Type | Display |
|---|---|
| `bool` | `"Yes"` / `"No"` |
| `DateTime` | `yyyy-MM-dd HH:mm` |
| `DateTimeOffset` | Local time `yyyy-MM-dd HH:mm` |
| Enum | `.ToString()` |
| `ValueAtom` | `.Value` (the inner string) |
| Collections | Hidden |
| `[AutoTableIgnoreRender]` | Hidden |
| `[AutoTableLink(propName, template)]` | Rendered as `MudLink` with URL from template (e.g. `"/products/{Id}"`) |

**Column labels** default to property name; override with `[AutoRenderLabel("text")]`.

**Usage:**

```razor
<AutoTable TItem="ProductDto"
           Items="_products"
           Selectable="true"
           SelectedItemsChanged="OnSelected"
           OnRowClick="HandleRowClick"/>
```

### 8.3 `AutoForm<TCommand>`

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `Model` | `TCommand` | The command/DTO to render |
| `IsReadOnly` | `bool` | Render all fields as read-only |
| `DefaultColumns` | `int` (default: `4`) | Grid columns (12 / DefaultColumns = item width), e.g. 4 → 3 items per row |
| `FieldStyle` | `Variant` (default: `Outlined`) | MudBlazor variant for inputs |

**Field type mapping:**

| CLR Type | MudBlazor Component | Notes |
|---|---|---|
| `bool` | `MudSwitch<bool>` | Color.Info |
| `DateTime` | `MudDatePicker` | `Clearable` if nullable |
| `DateTimeOffset` | `MudDatePicker` | Converts to/from local time |
| Enum | `MudSelect<T>` | Renders all enum values as `MudSelectItem`; supports nullable |
| `ValueAtom` (read-only) | `MudTextField<string>` read-only | Shows `.Value` |
| `ValueAtom` (editable, with options) | `MudSelect<ValueAtom>` | Options from `[AutoComponentValueAtomOptions]` |
| `ValueAtom` (editable, no options) | `MudTextField<string>` | Free text input |
| `string` with `[AutoFormRenderTextArea]` | `MudTextField<string>` | Multi-line with `AutoGrow` |
| Everything else | `MudTextField<string>` | Converts to/from string via `ConvertValue` |

**Non-nullable fields** get `*` appended to the label.

**Column width** is controlled per-property with `[AutoFormCellWidth(width)]` where width is 1-12 (MudBlazor grid column span).

**Hidden properties:** `[AutoFormIgnoreRender]`, `[AutoFormRenderReadOnly]`, collection types.

**Usage:**

```razor
<AutoForm Model="_command" DefaultColumns="3"/>
```

### 8.4 `AutoSearch<TQuery>`

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `Query` | `TQuery` | The search query object (mutated in place) |
| `SortQuery` | `SortQuery` | Sort state (mutated in place) |
| `SortFields` | `IReadOnlyList<string>` | Allowed sort field names |
| `OnSearch` | `EventCallback<TQuery>` | Fired on Search or Reset |

**Features:**
- Expandable panel with filter icon and header
- One field per property on `TQuery` (except `bool` and collection properties)
- Type-aware rendering: DatePicker for DateTime, MudSelect for Enum/ValueAtom/bool, TextField for everything else
- Sort dropdown + direction toggle (populated from `SortFields` parameter)
- Reset button clears all filters, sort, and re-queries
- Search/Refresh button: shows "Refresh" when all filters are null, "Search" otherwise
- All mutation is in-place on `Query` and `SortQuery` — no copies

**Usage:**

```razor
<AutoSearch TQuery="ProductSearchQuery"
            Query="_searchQuery"
            SortQuery="_sortQuery"
            SortFields="@(new MasterDataSortPreset().Build())"
            OnSearch="ExecuteSearch"/>
```

### 8.5 `AutoFormDialog<TModel>`

A `MudDialog` wrapper around `AutoForm` with built-in validation:

```razor
<AutoFormDialog TModel="ProductUpdateCommand"
                Model="_command"
                ValidateOnConfirm="true">
    <!-- optional ChildContent rendered after AutoForm fields -->
</AutoFormDialog>
```

- Cancel button → `MudDialog.Cancel()`
- Confirm button → runs `AutoValidatorService.ValidateAsync()`, then closes with `DialogResult.Ok(true)` on success

### 8.6 `AutoPicker<TItem, TQuery>`

A search-and-select dialog for picking entities:

```razor
<AutoPicker TItem="ProductDto"
            TQuery="ProductSearchQuery"
            Client="ProductClient"
            SortFields="_sortFields"
            MultipleSelect="true"/>
```

Features `AutoSearch` + `AutoTable` + `Pagination` in a dialog. Returns `IReadOnlyList<TItem>` as dialog data.

**Static helper for inline use:**

```csharp
var selected = await AutoPicker<RoleDto, RoleSearchQuery>.ShowAsync(
    DialogService, RoleClient, "Select Roles",
    initialQuery: new RoleSearchQuery { Status = MasterDataStates.Enabled });
```

---

## 9. MasterData Page System

For master data entities, the framework provides page base classes and toolbar components that handle the full CRUD + state + audit log workflow.

### 9.1 `MasterDataListPage<TDto, TSearch>`

Generic list page component.

**Parameters:**

| Parameter | Required | Description |
|---|---|---|
| `Title` | Yes | Page heading |
| `StateClient` | Yes | `IMasterDataStateController` for enable/disable/delete |
| `SearchClient` | Yes | `IPagedQueryController<TDto, TSearch>` for list and detail |
| `CreateHref` | No | "Add New" button link; hidden if null |
| `SortFields` | No | Sortable fields (default: `MasterDataSortPreset`) |
| `ToolBarActions` | No | Extra toolbar buttons (`RenderFragment`) |

**Renders:** ToolBar (title + batch actions + Add New + custom actions) → AutoSearch → AutoTable → Pagination.

**Usage:**

```razor
<MasterDataListPage TDto="ProductDto"
                    TSearch="ProductSearchQuery"
                    Title="Products"
                    CreateHref="/products/new"
                    StateClient="ProductClient"
                    SearchClient="ProductClient">
    <ToolBarActions>
        <MudButton Variant="Variant.Filled" Color="Color.Info" OnClick="ExportAll">
            Export
        </MudButton>
    </ToolBarActions>
</MasterDataListPage>
```

### 9.2 `MasterDataCreatePage<TDto, TSearch, TCreateCommand, TUpdateCommand>`

Generic create page component.

**Parameters:**

| Parameter | Required | Description |
|---|---|---|
| `Client` | Yes | `IMasterDataController` |
| `BackHref` | Yes | "Back" link target |
| `AfterCreateHref` | Yes | Navigation target after save; `{id}` is replaced with the new entity ID |
| `Title` | No | Default: "New" |
| `Command` | No | Pre-filled command (otherwise uses `new()`) |
| `ChildContent` | No | Extra form fields (`RenderFragment`) |

**Renders:** ToolBar (back link + title + Save) → Card (AutoForm + ChildContent).

**Usage:**

```razor
<MasterDataCreatePage TDto="ProductDto"
                      TSearch="ProductSearchQuery"
                      TCreateCommand="ProductCreateCommand"
                      TUpdateCommand="ProductUpdateCommand"
                      Client="ProductClient"
                      BackHref="/products"
                      AfterCreateHref="/products/{id}"
                      Title="New Product"/>
```

### 9.3 `MasterDataDetailPageBase<TDto, TSearch, TCreateCommand, TUpdateCommand>`

Abstract code-behind base class for detail pages. Your detail page inherits from this.

**Provided members:**

| Member | Type | Description |
|---|---|---|
| `Id` | `Guid` (parameter) | Route parameter |
| `Dto` | `TDto?` | Current entity data |
| `Client` | abstract property | Your API client |
| `RefreshData()` | virtual method | Calls `Client.GetAsync(Id)` |
| `OpenEdit()` | virtual method | Opens `AutoFormDialog` with `Dto.Adapt<TUpdateCommand>()` via Mapster, then calls `Client.UpdateAsync` on confirm |
| `OnBeforeOpenEdit(TUpdateCommand)` | virtual hook | Override to customize the command before the edit dialog opens |
| `EditChildContent(TUpdateCommand)` | virtual | Extra form fields in the edit dialog |
| `ExtraActions` | virtual | Additional toolbar action buttons |

**Subclass usage (from UserDetail.razor):**

```razor
@page "/products/{id:guid}"
@inherits MasterDataDetailPageBase<ProductDto, ProductSearchQuery,
    ProductCreateCommand, ProductUpdateCommand>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="pa-6">
    <MasterDataDetailPageToolBar TDto="ProductDto"
                                 Dto="Dto"
                                 Title="Product Detail"
                                 BackHref="/products"
                                 StateController="Client"
                                 LogController="Client"
                                 OnRefresh="RefreshData"
                                 OnEdit="OpenEdit"/>

    <MasterDataDetailPageFormCard TDto="ProductDto" Dto="Dto" Columns="3"/>
</MudContainer>

@code {
    protected override IMasterDataController<ProductDto, ProductSearchQuery,
        ProductCreateCommand, ProductUpdateCommand> Client => ProductClient;
}
```

### 9.4 `MasterDataDetailPageToolBar<TDto>`

Top toolbar for a detail page.

**Parameters:**

| Parameter | Description |
|---|---|
| `BackHref` | Back link target |
| `Title` | Page title |
| `Dto` | Current entity (null → hides action buttons) |
| `StateController` | For state transitions |
| `LogController` | For audit log |
| `Flow` | State flow (default: `MasterDataDefaultStatusFlow`) |
| `Guard` | Action guard (default: `MasterDataDefaultOperationGuard`) |
| `ExtraActions` | Additional action buttons |
| `OnRefresh` | Callback after actions |
| `OnEdit` | Edit button handler |
| `ExtraButtons` | Arbitrary extra buttons (`RenderFragment`) |

Renders: Back link → Title → Refresh → Action buttons (from `MasterDataFormActionToolBar`) → Extra buttons → Audit History toggle → `MasterDataLogDrawer`.

### 9.5 `MasterDataDetailPageFormCard<TDto>`

Read-only form card for a detail page:

```razor
<MasterDataDetailPageFormCard TDto="ProductDto"
                              Dto="Dto"
                              Columns="3">
    <!-- optional ChildContent invoked with Dto -->
</MasterDataDetailPageFormCard>
```

Renders a `MudCard` with `AutoForm` in read-only mode. Falls back to a "Failed to Fetch Data" alert if `Dto` is null.

### 9.6 `MasterDataTableActionToolBar<TItem>`

Batch action toolbar for list pages (multi-select). Built-in actions: Enable, Disable, Delete. Actions are gated by `StateFlowBase` (Enable/Disable) or `StateActionGuardBase` (Delete). The Delete action includes a confirmation dialog.

Supports `ExtraActions` for custom batch operations (example from UserList):

```razor
<MasterDataTableActionToolBar TItem="UserDto"
                              Client="UsersClient"
                              SelectedItems="_selectedUsers"
                              Flow="new MasterDataDefaultStatusFlow()"
                              Guard="new MasterDataArchiveOperationGuard()"
                              ExtraActions="ExtraActions"
                              OnDataChanged="LoadPagedResult"/>
```

### 9.7 `MasterDataFormActionToolBar`

Single-entity action toolbar for detail pages. Built-in: Enable, Disable, Delete, Edit. Same gating logic as the table version but operates on a single entity (passes `Id` and `Status`). Delete triggers navigation to `AfterDeleteNavTarget`.

### 9.8 `MasterDataLogDrawer`

Right-side drawer displaying the audit log as a `MudTimeline`. Accepts a `FetchLogsAsync` delegate matched to `IMasterDataLogController.GetAuditLogsAsync`. Called by `MasterDataDetailPageToolBar` via the "Audit History" button.

---

## 10. Shared Components

### 10.1 `Pagination`

```razor
<Pagination Query="_paginationQuery"
            OnChanged="LoadData"
            TotalItems="_result.TotalCount"/>
```

Features: rows per page selector (10/20/50/100/500), first/prev/page numbers/next/last navigation, windowed page numbers with ellipsis. All mutations are in-place on the `PaginationQuery`.

### 10.2 `EditableTable<TItem>`

An `AutoTable` with inline Add/Edit/Delete via `AutoFormDialog`:

- **Add**: creates `new TItem()`, opens dialog, appends to `Items` on confirm
- **Edit**: shallow-copies the item via `MemberwiseClone`, opens dialog, copies properties back on confirm
- **Delete**: multi-select + confirmation dialog

Events: `OnRowAdded`, `OnRowCommitted`, `OnRowDeleted`.

### 10.3 `SubEntityBatchTable<TItem>`

A card with an `AutoTable` + `Pagination` for managing sub-entity relationships (e.g., roles assigned to a user):

```razor
<SubEntityBatchTable TItem="RoleDto"
                     Title="User Belongs to Roles:"
                     OnAssignClicked="BatchAssignRoles"
                     OnBatchDelete="BatchDeleteRoles"
                     FetchPagedAsync="q => UserClient.GetRolesAsync(Id, q)"
                     AssignLabel="Batch Assign"
                     DeleteLabel="Batch Delete"/>
```

Supports both paged (`FetchPagedAsync`) and simple (`FetchSimpleAsync`) data sources. Exposes `RefreshAsync()` for external refresh.

### 10.4 `PickerInput<TItem, TQuery>`

A read-only `MudTextField` with a search icon that opens an `AutoPicker` dialog:

```razor
<PickerInput TItem="ProductDto"
             TQuery="ProductSearchQuery"
             Client="ProductClient"
             Label="Product"
             DisplayText="@SelectedProduct?.Name"
             OnSelected="HandleProductSelected"
             Clearable="true"/>
```

Displays the selected item's text, reopens picker on adornment click, clears on clear button.

### 10.5 `GenericListPage<TDto, TSearch>`

Simpler version of `MasterDataListPage` without the MasterData-specific features (no status column, no state toolbar):

```razor
<GenericListPage TDto="TagDto"
                 TSearch="TagSearchQuery"
                 Title="Tags"
                 SearchClient="TagClient"
                 SortFields="_sortFields">
    <ToolBarActions>...</ToolBarActions>
</GenericListPage>
```

### 10.6 `GlobalErrorBoundary`

Wraps `Microsoft.AspNetCore.Components.Web.ErrorBoundary`. On exception, renders a full-screen overlay with the error message, expandable stack trace, and a "Refresh" button that force-reloads the home page.

---

## 11. Other Services

### 11.1 `AutoValidatorService`

```csharp
public class AutoValidatorService(IDialogService dialogService)
{
    public async Task<bool> ValidateAsync<T>(T model, params string[] additionalErrors) where T : class;
}
```

Wraps `AutoValidator.Validate()` (from Abstraction) and displays validation errors as a MudBlazor message box. Returns `true` if valid, `false` and shows dialog if not. Supports additional error messages injected at call time.

Used by `AutoFormDialog` and by manual save handlers (e.g., `UserCreate.razor` calls `AutoValidator.ValidateAsync` directly).

### 11.2 `AlcedoLogo`

An inline SVG component (a stylized bird/owl logo). Parameters: `Width`, `Height` (default 32x32), plus `CaptureUnmatchedValues` for additional SVG attributes.

---

## 12. Typical Page Anatomy

### Simple list page (reusing `MasterDataListPage`)

```razor
@page "/notification/templates"
@inject IMessageTemplate Client

<MasterDataListPage TDto="MessageTemplateDto"
                    TSearch="MessageTemplateSearchQuery"
                    Title="Templates"
                    CreateHref="/notification/templates/new"
                    StateClient="Client"
                    SearchClient="Client"/>
```

14 lines of Razor gives you: search, sortable table, pagination, multi-select, enable/disable/delete with confirmation, and "Add New" button.

### Custom list page (with extra toolbar actions)

```razor
@page "/identity/users"
@inject IUserClient UsersClient

<MasterDataListPage TDto="UserDto"
                    TSearch="UserSearchQuery"
                    Title="Users"
                    CreateHref="/identity/users/new"
                    StateClient="UsersClient"
                    SearchClient="UsersClient">
    <ToolBarActions>
        <MudButton Variant="Variant.Filled"
                   StartIcon="@Icons.Material.Filled.Archive"
                   Color="Color.Secondary"
                   OnClick="ArchiveSelected">Archive</MudButton>
    </ToolBarActions>
</MasterDataListPage>
```

### Detail page (inheriting `MasterDataDetailPageBase`)

Covered in section 9.3. The base class handles data loading, edit dialog, and state management. Override `ExtraActions` for domain-specific actions.
