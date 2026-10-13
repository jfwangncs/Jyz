# Implement a Backend Feature Module

Tool-agnostic step-by-step guide for implementing a complete full-stack feature module (backend + frontend) in this repository: adding a new entity, CRUD endpoints, service, or any module in `src/api` AND the corresponding Vue frontend. Covers DB schema design with user review, Entity + AppDbContext, Request/Response DTOs, FluentValidation, ErrorCode assignment (4000+ range), Options config, Service interface + implementation (JfYu.Data), DI registration, Controller (CustomController), Vue router, API file (requestClient), VxeGrid list page, drawer form, and i18n locales (zh-CN + en-US system/page JSON; error messages are localized on the backend).

Any AI coding agent (or human) implementing a feature in the dotnet backend + Vue frontend should follow this workflow **in order**, pausing for user review at marked checkpoints.

---

## Module Type: CRUD vs Read-Only vs Client-Side Paged

Before starting, identify the module type:

| Type                  | Description                                                                              | Examples                        |
| --------------------- | ---------------------------------------------------------------------------------------- | -------------------------------- |
| **CRUD**              | Full create/read/update workflow                                                         | User, Role, Permission, Product |
| **Read-Only (Log)**   | No create/update/delete via API; data is written internally by the system                | AuditLog, LoginLog              |
| **Client-Side Paged** | Backend returns all items at once (no server pagination); frontend filters and paginates | Redis Cache keys                |

**If Client-Side Paged**:

- **Skip** the server-side `QueryRequest` / `GetPagedAsync` 鈥?the API returns `List<T>` or equivalent
- **Frontend `query`**: fetch all items, apply client-side filter (`Array.filter`), slice for the current page
- **`pagerConfig`**: set `{ pageSize: 20, pageSizes: [10,20,50,100] }` explicitly in grid options
- **`checkboxConfig`**: specify `{ key: '<fieldName>' }` if the keyField is not `id`
- **`rowConfig`**: set `{ keyField: '<fieldName>' }` to match the checkbox key
- **Batch delete**: use `gridApi.grid?.getCheckboxRecords()` to get selected rows; pass their key values to a delete API

**If Read-Only (Log)**:

- **Skip** Step 3 `Create<Name>Request` and `Update<Name>Request` 鈥?only `<Name>Response` needed
- **Skip** Step 4 Validations 鈥?nothing to validate on input
- **Service interface**: only query methods, no `IService<T, TContext>` inheritance needed (or inherit but expose no write methods)
- **Controller**: only `GET` (list + detail), no `POST`/`PUT`. Use `[Authorize]` instead of write-action `[Permission]` codes
- **Frontend**: no "Create" button in toolbar, no `form.vue` drawer, list page is read-only
- **Checklist**: skip all create/update items

---

## Step 1 鈥?Analyze Database Requirements

1. Determine if a **new table** or **modification to an existing table** is needed.
2. List the proposed fields with:
   - Field name (PascalCase)
   - C# type (string, int, decimal, DateTime, bool, enum, 鈥?
   - Constraints: `[Required]`, `[MaxLength(n)]`, nullable `?`, default value
   - Relationships: navigation properties and FK conventions
3. **Note**: `BaseEntity` already provides `Id` (int), `Status`, `CreatedTime`, `UpdatedTime` 鈥?do NOT redeclare these.

> **鈴?CHECKPOINT 鈥?Present fields to the user for review before coding.**
> Ask: "Are these fields correct? Any changes, additions, or removals?"

---

## Step 2 鈥?Create / Modify Entity

**New entity** 鈫?create `src/api/Jyz/Entity/<Name>.cs`:

```csharp
using JfYu.Data.Model;
using System.ComponentModel.DataAnnotations;

namespace JfYu.WebApi.Template.Entity
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        // navigation properties as needed
    }
}
```

**Register in AppDbContext** 鈫?add `DbSet<T>` to `Entity/AppDbContext.cs`:

```csharp
public DbSet<Product> Products { get; set; }
```

**Add EF Core migration** (remind the user to run after review):

```bash
dotnet ef migrations add Add<Name> --project src/api/Jyz
dotnet ef database update --project src/api/Jyz
```

---

## Step 3 鈥?Create DTOs

Create a **module folder** under `src/api/Jyz/Model/<Name>/` and place all DTOs there:

| File                                   | Purpose                                       |
| --------------------------------------- | ---------------------------------------------- |
| `Model/<Name>/Create<Name>Request.cs`  | Fields for creation                           |
| `Model/<Name>/Update<Name>Request.cs`  | Fields for update (Id + changeable fields)    |
| `Model/<Name>/Feature<Name>Request.cs` | Fields for business logic (if needed)         |
| `Model/<Name>/<Name>Response.cs`       | Fields returned to client (no sensitive data) |

Example for a `Product` module:

```
Model/
  Product/
    CreateProductRequest.cs
    UpdateProductRequest.cs
    ProductResponse.cs
```

Namespace convention: `JfYu.WebApi.Template.Model.<Name>` (matches the folder, e.g. `JfYu.WebApi.Template.Model.Product`).

Use plain C# classes 鈥?no inheritance needed.

### Update request 鈥?nullable fields / partial update contract

**All fields in `Update<Name>Request` must be nullable.** The frontend only sends fields that actually changed; the controller applies only the fields that are non-null.

```csharp
// 鉁?Correct 鈥?every field nullable
public class UpdateProductRequest
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public int? Status { get; set; }
}
```

Controller apply-pattern 鈥?guard every field before assigning:

```csharp
if (request.Name != null)        item.Name = request.Name;
if (request.Price.HasValue)      item.Price = request.Price.Value;
if (request.Status.HasValue)     item.Status = request.Status.Value;
```

Frontend `UpdateParams` mirrors this: every property is optional (`?`). Callers only include changed keys in the payload 鈥?omitted keys are simply not sent.

```ts
export interface UpdateProductParams {
  name?: string;
  price?: number;
  status?: number;
}
```

> This means a status-only toggle (`{ status: 0 }`) and a full form save both use the same `PUT /{id}` endpoint 鈥?**no separate toggle endpoint is needed**.

---

## Step 4 鈥?Create Validations

Create validation files in `src/api/Jyz/Validations/` for each request DTO:

- `Create<Name>RequestValidation.cs`
- `Update<Name>RequestValidation.cs`
- `Feature<Name>RequestValidation.cs` (if applicable)

```csharp
using FluentValidation;
using JfYu.WebApi.Template.Model.Product;

namespace JfYu.WebApi.Template.Validations
{
    public class CreateProductRequestValidation : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidation()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThan(0);
        }
    }
}
```

Use the module namespace `JfYu.WebApi.Template.Model.<Name>` matching the DTO folder.

Validators are **auto-discovered** 鈥?no manual registration needed.

---

## Step 5 鈥?Assign Error Codes

Open `src/api/Jyz/Constants/ErrorCode.cs`.

**Current module ranges** (4000+ space, 50鈥?00 codes per module):

| Range     | Module                |
| --------- | ---------------------- |
| 4000鈥?049 | General client errors |
| 4100鈥?149 | Auth                  |
| 4150鈥?199 | User                  |
| 4200鈥?249 | Role                  |
| 4250鈥?299 | Permission            |
| 4300+     | Other / unassigned    |

**Decision logic**:

- Error clearly belongs to an **existing module** 鈫?add under that module's `#region`, continuing its sequential values. If it overflows, let the user know.
- Error is for a **new module** 鈫?start a new `#region` at the next available 50-step boundary (e.g., 4250, 4300 if free) and comment the range.
- Ambiguous 鈫?assign to the closest existing module.

**Pattern**:

```csharp
#region Product
ProductNotFound = 4250,

DuplicateProduct,
#endregion
```

Then add the display text to the resource files (keys follow `ErrorCode.{MemberName}`):

`src/api/Jyz/Resources/EnumMessages.resx` (English):

```xml
<data name="ErrorCode.ProductNotFound" xml:space="preserve">
  <value>Product not found.</value>
</data>
```

`src/api/Jyz/Resources/EnumMessages.zh-CN.resx` (Chinese):

```xml
<data name="ErrorCode.ProductNotFound" xml:space="preserve">
  <value>鍟嗗搧涓嶅瓨鍦ㄣ€?/value>
</data>
```

To add another language (e.g., French, German), add `EnumMessages.fr.resx` / `EnumMessages.de.resx` with the same keys and register the culture in `AddCustomLocalization()`.

---

## Step 6 鈥?Options Configuration (if needed)

Only when the feature requires settings stored in `appsettings.json` (no secrets/passwords).

1. Create `src/api/Jyz/Options/<Name>Settings.cs`:

```csharp
namespace JfYu.WebApi.Template.Options
{
    public class ProductSettings
    {
        public int MaxItemsPerPage { get; set; } = 50;
    }
}
```

2. Register in `Extensions/OptionsExtension.cs`:

```csharp
services.Configure<ProductSettings>(configuration.GetSection("ProductSettings"));
```

3. Inject via `IOptions<ProductSettings>` in services/controllers.

---

## Step 7 鈥?Create Service Interface + Implementation

### Interface: `Services/Interfaces/I<Name>Service.cs`

- **With DB access** 鈫?inherit `IService<TEntity, AppDbContext>`:

```csharp
public interface IProductService : IService<Product, AppDbContext>
{
    Task<PagedData<Product>> GetPagedAsync(QueryRequest query);
}
```

- **Without DB access** 鈫?plain interface, no inheritance.

### Implementation: `Services/<Name>Service.cs`

- **With DB access** 鈫?inherit `Service<TEntity, AppDbContext>`:

> **Note**: The base class `Service<TEntity, AppDbContext>` provides two protected fields:
>
> - `_context` 鈥?the write `AppDbContext` (for INSERT / UPDATE / DELETE + `SaveChangesAsync`)
> - `_readonlyContext` 鈥?the read-only `ReadonlyDBContext<AppDbContext>` (for queries, no tracking)
>
> Also inherits: `AddAsync`, `UpdateAsync`, `RemoveAsync` (soft delete), `HardRemoveAsync` (hard delete), `GetOneAsync`, `GetListAsync`, `GetSelectListAsync` 鈥?do **NOT** re-implement these. `GetPagedAsync` is **not** inherited 鈥?define it per service using `IQueryable.ToPagedAsync()`.

```csharp
using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Service;
using Mapster;
using JfYu.WebApi.Template.Entity;
using JfYu.WebApi.Template.Exceptions;
using JfYu.WebApi.Template.Constants;
using JfYu.WebApi.Template.Model;
using JfYu.WebApi.Template.Model.Product;
using JfYu.WebApi.Template.Services.Interfaces;

namespace JfYu.WebApi.Template.Services
{
    public class ProductService(AppDbContext context, ReadonlyDBContext<AppDbContext> readonlyDBContext)
        : Service<Product, AppDbContext>(context, readonlyDBContext), IProductService
    {
        public async Task<PagedData<Product>> GetPagedAsync(QueryRequest query)
        {
            var q = _readonlyContext.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchKey))
                q = q.Where(p => p.Name.Contains(query.SearchKey));

            if (query.Status.HasValue)
                q = q.Where(p => p.Status == query.Status.Value);

            if (query.StartTime.HasValue)
                q = q.Where(p => p.CreatedTime >= query.StartTime.Value);

            if (query.EndTime.HasValue)
                q = q.Where(p => p.CreatedTime <= query.EndTime.Value);

            return await q.ToPagedAsync(query.PageIndex, query.PageSize);
        }
    }
}
```

> **Controller is responsible for DTO mapping**: The service returns `PagedData<TEntity>` (from JfYu.Data).
> The controller calls `.Adapt<PagedResult<TResponse>>()` to convert before returning:
>
> ```csharp
> var result = await _productService.GetPagedAsync(query);
> return Ok(result.Adapt<PagedResult<ProductResponse>>());
> ```

- **Without DB access** 鈫?plain class implementing the interface.

**Business errors** 鈫?throw `BusinessException(ErrorCode.XxxError)` 鈥?the global handler converts these to structured `BadRequest` responses automatically.
Every business error needs to map to one error code.

---

## Step 8 鈥?Register DI

Add to `Extensions/InjectionExtension.cs` inside `AddCustomInjection()`:

```csharp
//#if (EnableRBAC)
services.AddScoped<IProductService, ProductService>();
//#endif
```

**Feature flag rules** 鈥?wrap DI registration in the appropriate flag:

| Scenario                                  | Flag                   |
| ------------------------------------------ | ----------------------- |
| Service needs EF Core / DB (most modules) | `#if (EnableRBAC)`     |
| JWT-only service (e.g. token helpers)     | `#if (EnableJWT)`      |
| WeChat-specific service                   | `#if (EnableWeChat)`   |
| Redis-backed service                      | `#if (EnableJWTRedis)` |
| Pure utility service (no external deps)   | No flag needed         |

> Most new feature modules use EF Core 鈫?wrap in `#if (EnableRBAC)`.

---

## Step 9 鈥?Permission Codes

Open `src/api/Jyz/Constants/PermissionCodes.cs` and add constants for the new module.

**Convention**:

- Module entry (Menu) 鈫?`public const string <Name> = "<name>";` 鈥?**value is lowercase**
- Each action 鈫?`public const string <Name><Action> = "<name>:<action>";` 鈥?**both parts lowercase**

```csharp
public const string Product = "product";
public const string ProductGet = "product:get";
public const string ProductAdd = "product:add";
public const string ProductEdit = "product:edit";
public const string ProductDelete = "product:delete";
```

**Current modules in `PermissionCodes.cs`** (for reference):

| Constant group | String values                                                                                                           |
| --------------- | ------------------------------------------------------------------------------------------------------------------------- |
| System         | `"system"`                                                                                                              |
| Role           | `"role"`, `"role:add"`, `"role:get"`, `"role:edit"`, `"role:assign"`                                                    |
| Permission     | `"permission"`, `"permission:get"`, `"permission:add"`, `"permission:edit"`, `"permission:delete"`, `"permission:sync"` |
| User           | `"user"`, `"user:get"`, `"user:edit"`                                                                                   |

**Parent code** determines where this module appears in the permission tree:

- Check existing top-level constants (e.g., `System = "system"`, `Dashboard = "dashboard"`).
- If the module clearly belongs to an existing parent 鈫?use it directly.
- If uncertain or no suitable parent exists 鈫?**ask the user** before proceeding.

> **鈴?CHECKPOINT 鈥?Confirm parent code with user if not obvious.**
> Ask: "This module's menu permission will be placed under `PermissionCodes.System`. Is that correct, or should it be under a different parent?"

> **鈴?CHECKPOINT 鈥?Before writing controller, confirm with user:**
> "Here is the planned permission code set: `product`, `product:get`, `product:add`, `product:edit`. Does this look right? Any actions to add or remove?"

---

## Step 10 鈥?Create Controller

Create `Controllers/<Name>Controller.cs`.

- The **class** gets `[Permission(PermissionCodes.<Name>, PermissionType.Menu, parentCode: PermissionCodes.<Parent>)]` 鈥?this registers it as a menu entry.
- Each **action method** gets `[Permission(PermissionCodes.<Name><Action>)]` 鈥?type defaults to `Button`.

```csharp
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Mapster;
using JfYu.WebApi.Template.Attributes;
using JfYu.WebApi.Template.Constants;
using JfYu.WebApi.Template.Model;
using JfYu.WebApi.Template.Model.Product;
using JfYu.WebApi.Template.Services.Interfaces;

namespace JfYu.WebApi.Template.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Permission(PermissionCodes.Product, PermissionType.Menu, parentCode: PermissionCodes.System)]
    public class ProductController(IProductService productService) : CustomController
    {
        private readonly IProductService _productService = productService;

        [HttpGet]
        [Permission(PermissionCodes.ProductGet)]
        public async Task<IActionResult> GetAllAsync([FromQuery] QueryRequest query)
        {
            var result = await _productService.GetPagedAsync(query);
            return Ok(result.Adapt<PagedResult<ProductResponse>>());
        }

        [HttpGet("{id}")]
        [Permission(PermissionCodes.ProductGet)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var item = await _productService.GetOneAsync(q => q.Id == id);
            if (item == null)
                return BadRequest(ErrorCode.ProductNotFound);
            return Ok(item.Adapt<ProductResponse>());
        }

        [HttpPost]
        [Permission(PermissionCodes.ProductAdd)]
        public async Task<IActionResult> CreateAsync([FromBody][Required] CreateProductRequest request)
        {
            // check duplicates if needed, then adapt and save
            var item = request.Adapt<Product>();
            await _productService.AddAsync(item);
            return Ok();
        }

        [HttpPut("{id}")]
        [Permission(PermissionCodes.ProductEdit)]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody][Required] UpdateProductRequest request)
        {
            var item = await _productService.GetOneAsync(q => q.Id == id);
            if (item == null)
                return BadRequest(ErrorCode.ProductNotFound);
            // apply nullable fields
            if (request.Name != null) item.Name = request.Name;
            if (request.Price.HasValue) item.Price = request.Price.Value;
            if (request.Status.HasValue) item.Status = request.Status.Value;
            await _productService.UpdateAsync(item);
            return Ok();
        }
    }
}
```

**Conventions**:

- Always return `IActionResult` 鈥?never `ActionResult<T>` or raw objects.
- Use `Ok<T>(data)` / `Ok()` / `BadRequest(ErrorCode)` from `CustomController`.
- Do NOT use `[Authorize]` directly 鈥?`[Permission]` handles authentication + authorization.
- All methods must be async 鈥?no `.Result` or `.Wait()`.
- Add `[Required]` on `[FromBody]` parameters.

---

## Step 11 鈥?Frontend: Router

> **Modification boundary (Vben Admin):** All business code MUST live under `src/web/apps/web-antd/src/`. Never edit `src/web/packages/**`, `src/web/internal/**`, `src/web/scripts/**`, or `src/web/apps/web-antd/src/{main.ts,bootstrap.ts,preferences.ts,layouts/**}` 鈥?those are upstream Vben framework files and modifying them makes future framework upgrades painful. Extend the framework only via `apps/web-antd/src/adapter/**` or by wrapping in business components under `apps/web-antd/src/components/`. See [src/web/AGENTS.md](../src/web/AGENTS.md) 搂"Modification Boundaries" for the full policy.

Check `src/web/apps/web-antd/src/router/routes/modules/`.

- **Existing domain file** (e.g., `system.ts`) 鈫?add a child route entry.
- **New domain** 鈫?create `<domain>.ts` and add its default export to `src/router/routes/index.ts`.

Route pattern:

```ts
{
  name: 'ProductManagement',           // PascalCase, unique
  path: '/system/product',
  component: () => import('#/views/system/product/index.vue'),
  meta: {
    icon: 'lucide:package',            // Lucide icon name
    title: $t('page.system.product'),  // i18n key
  },
},
```

---

## Step 12 鈥?Frontend: API File

Create `src/web/apps/web-antd/src/api/system/<domain>.ts`:

> **Note**: API files for features live in `src/api/system/`, **not** `src/api/core/`.
> Export chain: `src/api/system/<domain>.ts` 鈫?`src/api/system/index.ts` 鈫?re-exported by `src/api/core/index.ts` (via `export * from '../system'`) 鈫?`src/api/index.ts` (via `export * from './core'`)

```ts
import { requestClient } from "#/api/request";

export namespace SystemProductApi {
  export interface SystemProduct {
    id: number;
    name: string;
    price: number;
    status: number;
    createdTime: string;
  }

  export interface CreateProductParams {
    name: string;
    price: number;
  }

  export interface UpdateProductParams {
    name?: string;
    price?: number;
    status?: number;
  }
}

export async function getProductList(params: Record<string, any>) {
  return requestClient.get<{
    items: SystemProductApi.SystemProduct[];
    total: number;
  }>("/product", { params });
}

export async function createProduct(
  data: SystemProductApi.CreateProductParams,
) {
  return requestClient.post("/product", data);
}

export async function updateProduct(
  id: number,
  data: SystemProductApi.UpdateProductParams,
) {
  return requestClient.put(`/product/${id}`, data);
}

export async function deleteProduct(id: number) {
  return requestClient.delete(`/product/${id}`);
}
```

Then re-export from `src/api/system/index.ts`:

```ts
export * from "./<domain>";
```

---

## Step 13 鈥?Frontend: Views (based on Role module template)

Create three files mirroring the role module structure:

### `src/views/<domain>/<name>/data.ts`

Defines form schemas and column configs using `$t()` for all labels:

```ts
import type { VbenFormSchema } from "#/adapter/form";
import type { OnActionClickFn, VxeTableGridOptions } from "#/adapter/vxe-table";
import { $t } from "#/locales";

export function useFormSchema(): VbenFormSchema[] {
  return [
    {
      component: "Input",
      fieldName: "name",
      label: $t("system.product.name"),
      rules: "required",
    },
    // ...
  ];
}

export function useGridFormSchema(): VbenFormSchema[] {
  /* search bar schema */
}

export function useColumns<T = SystemProductApi.SystemProduct>(
  onActionClick: OnActionClickFn<T>,
  onStatusChange?: (newStatus: any, row: T) => PromiseLike<boolean | undefined>,
): VxeTableGridOptions["columns"] {
  return [
    // ... other columns ...
    {
      // Status toggle column 鈥?use CellSwitch when onStatusChange is provided, CellTag otherwise
      cellRender: {
        attrs: { beforeChange: onStatusChange },
        name: onStatusChange ? "CellSwitch" : "CellTag",
      },
      field: "status",
      title: $t("system.product.status"),
      width: 120,
    },
    // ... operation column ...
  ];
}
```

### `src/views/<domain>/<name>/modules/form.vue`

Drawer form for create/edit 鈥?uses `useVbenForm` + `useVbenDrawer`:

```vue
<script lang="ts" setup>
import { ref } from "vue";
import { useVbenForm, useVbenDrawer } from "@vben/common-ui";
import { createProduct, updateProduct } from "#/api";
import { useFormSchema } from "../data";

const emits = defineEmits(["success"]);
const id = ref<number>();

const [Form, formApi] = useVbenForm({
  schema: useFormSchema(),
  showDefaultActions: false,
});
const [Drawer, drawerApi] = useVbenDrawer({
  async onConfirm() {
    const { valid } = await formApi.validate();
    if (!valid) return;
    const values = await formApi.getValues();
    drawerApi.lock();
    (id.value ? updateProduct(id.value, values) : createProduct(values))
      .then(() => {
        emits("success");
        drawerApi.close();
      })
      .catch(() => drawerApi.unlock());
  },
  async onOpenChange(isOpen) {
    if (!isOpen) return;
    formApi.resetForm();
    const data = drawerApi.getData<SystemProductApi.SystemProduct>();
    id.value = data?.id;
    if (data) {
      await nextTick();
      formApi.setValues(data);
    }
  },
});
</script>
```

### `src/views/<domain>/<name>/index.vue`

Main list page 鈥?uses `useVbenVxeGrid` with proxy config pointing to the list API:

> **Critical**: Always use `<Page auto-content-height>` on list pages that use `height: 'auto'` on VxeGrid.
> Without `auto-content-height`, the grid has no fixed-height parent and enters an infinite ResizeObserver
> feedback loop, causing `vxe-table--row-expanded-wrapper` height to grow endlessly.

> **Expand row rule**: When using row expansion (`expandConfig`), the expand column **must** declare
> `slots: { content: 'expand' }` explicitly, otherwise the `#expand` slot in the template won't connect
> and the expanded content will be blank:
>
> ```ts
> { type: 'expand', width: 50, slots: { content: 'expand' } }
> ```
>
> ```vue
> <template #expand="{ row }">
>   <MyPanel :row="row" />
> </template>
> ```

> **Toolbar visibility rule**: The VxeGrid toolbar only renders when it has content 鈥?either a
> `table-title` prop OR a `#toolbar-actions` slot. Without one of these, the built-in `refresh`,
> `search`, and `custom` buttons will NOT appear even if set in `toolbarConfig`.
>
> - CRUD pages: have `#toolbar-actions` (Create button) 鈫?toolbar renders automatically.
> - Read-Only (Log) pages: **must** add `:table-title="$t('system.<module>.list')"` on `<Grid>`.

> **Icons**: Use named Lucide icon components from `@vben/icons` (e.g. `Plus`, `Pencil`).
> Do NOT import from `@ant-design/icons-vue` 鈥?that package is not available in this project.

> **Toolbar refresh rule**: Always set `refresh: true` in `toolbarConfig` 鈥?this uses VxeGrid's built-in
> refresh button. **Never** add a manual `<Button>` with `RotateCw` in `#toolbar-actions` for refresh.
> The `#toolbar-actions` slot is ONLY for action buttons like "Create".
>
> **`gridApi` rule**:
>
> - CRUD pages: destructure `[Grid, gridApi]` 鈥?`gridApi.query()` is called via `onRefresh()` when a form drawer emits `success`.
> - Read-only (Log) pages: destructure `[Grid]` only 鈥?no `gridApi`, no `onRefresh`, no `#toolbar-actions` slot.

**CRUD module** (`index.vue` with Create button):

```vue
<script lang="ts" setup>
import { Page, useVbenDrawer } from "@vben/common-ui";
import { Plus } from "@vben/icons";
import { Button, Modal } from "ant-design-vue";
import { useVbenVxeGrid } from "#/adapter/vxe-table";
import { getProductList, updateProduct } from "#/api";
import { $t } from "#/locales";
import { useColumns, useGridFormSchema } from "./data";
import Form from "./modules/form.vue";

const [FormDrawer, formDrawerApi] = useVbenDrawer({
  connectedComponent: Form,
  destroyOnClose: true,
});
const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns: useColumns(onActionClick, onStatusChange),
    height: "auto",
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) =>
          getProductList({
            pageIndex: page.currentPage,
            pageSize: page.pageSize,
            ...formValues,
          }),
      },
    },
    rowConfig: { keyField: "id" },
    toolbarConfig: {
      custom: true,
      export: false,
      refresh: true, // 鈫?built-in refresh button; never add a manual one
      search: true,
      zoom: false,
    },
  },
  formOptions: { schema: useGridFormSchema(), submitOnChange: true },
});

function onActionClick(e) {
  if (e.code === "edit") formDrawerApi.setData(e.row).open();
}

function confirm(content: string, title: string) {
  return new Promise((resolve, reject) => {
    Modal.confirm({
      content,
      onCancel() {
        reject(new Error("cancelled"));
      },
      onOk() {
        resolve(true);
      },
      title,
    });
  });
}

// Status toggle 鈥?uses i18n keys from system.common (prefix-change / mid-change / suffix-change)
async function onStatusChange(
  newStatus: number,
  row: SystemProductApi.SystemProduct,
): Promise<boolean> {
  const label = newStatus === 1 ? $t("common.enabled") : $t("common.disabled");
  try {
    await confirm(
      `${$t("system.common.prefix-change")} ${row.name} ${$t("system.common.mid-change")} ${$t("system.product.status")} ${$t("system.common.suffix-change")}銆?${label}銆?`,
      $t("common.edit"),
    );
    await updateProduct(row.id, { status: newStatus });
    return true;
  } catch {
    return false;
  }
}

function onRefresh() {
  gridApi.query();
}
</script>

<template>
  <Page auto-content-height>
    <FormDrawer @success="onRefresh" />
    <Grid>
      <template #toolbar-actions>
        <!-- Only action buttons here (e.g. Create). Never add a refresh button. -->
        <Button type="primary" @click="formDrawerApi.open()">
          <Plus class="size-5" />
          {{ $t("common.create") }}
        </Button>
      </template>
    </Grid>
  </Page>
</template>
```

**Read-Only (Log) module** (`index.vue` 鈥?no create button, no gridApi):

```vue
<script lang="ts" setup>
import { Page } from "@vben/common-ui";
import { useVbenVxeGrid } from "#/adapter/vxe-table";
import { getProductLogList } from "#/api";
import { useColumns, useGridFormSchema } from "./data";

const [Grid] = useVbenVxeGrid({
  // 鈫?no gridApi destructured
  gridOptions: {
    columns: useColumns(),
    height: "auto",
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) =>
          getProductLogList({
            pageIndex: page.currentPage,
            pageSize: page.pageSize,
            ...formValues,
          }),
      },
    },
    rowConfig: { keyField: "id" },
    toolbarConfig: {
      custom: true,
      export: false,
      refresh: true, // 鈫?built-in refresh; no toolbar-actions slot needed
      search: true,
      zoom: false,
    },
  },
  formOptions: { schema: useGridFormSchema(), submitOnChange: true },
});
</script>

<template>
  <Page auto-content-height>
    <!-- table-title is required for read-only pages 鈥?it makes the toolbar render -->
    <Grid :table-title="$t('system.productLog.list')" />
  </Page>
</template>
```

---

## Step 14 鈥?Frontend: i18n Locales

> **Error messages**: The backend returns a localized `message` (from `src/api/Jyz/Resources/EnumMessages.*.resx`, resolved by `Accept-Language`). Do **not** create frontend `error.json` files 鈥?the error toast in `request.ts` displays the backend `message` directly.

Update the locale files:

### UI labels 鈥?field names, table headers, drawer titles

`src/locales/langs/zh-CN/system.json` 鈥?add module key under the domain object:

```json
{
  "product": {
    "title": "鍟嗗搧绠＄悊",
    "list": "鍟嗗搧鍒楄〃",
    "name": "鍟嗗搧鍚嶇О",
    "price": "浠锋牸",
    "status": "鐘舵€?,
    "createdTime": "鍒涘缓鏃堕棿",
    "operation": "鎿嶄綔"
  }
}
```

`src/locales/langs/en-US/system.json` 鈥?English equivalent.

### Page/menu titles (only if new route was added)

`src/locales/langs/zh-CN/page.json` 鈥?add to corresponding domain:

```json
{
  "system": {
    "product": "鍟嗗搧绠＄悊"
  }
}
```

`src/locales/langs/en-US/page.json` 鈥?English equivalent.

---

## Step 15 鈥?Frontend: Unit Tests

After the frontend code is in place, **add unit tests** for the testable business logic. Tests live alongside the code they cover under `__tests__/` folders and are picked up automatically by `pnpm test:unit` (vitest from the workspace root).

### What to test (priority order)

1. **Pure logic in `data.ts`** 鈥?tree builders (e.g. `buildParentTree`), option factories, formatters. These are the highest-value tests because they have no UI dependencies.
2. **API wrappers in `src/api/system/<domain>.ts`** 鈥?verify the correct HTTP method, URL, and payload are passed to `requestClient` via `vi.mock`.
3. **Domain helpers** 鈥?any non-trivial transformation that lives outside Vue components.

> **Out of scope** for this guide: full Vue component / drawer / grid integration tests. Those typically require mounting heavy vben primitives and offer poor ROI on a CRUD scaffold.

### File locations

```
apps/web-antd/src/views/system/<name>/__tests__/data.test.ts
apps/web-antd/src/api/system/__tests__/<domain>.test.ts
```

### `data.ts` test pattern

`data.ts` files import `$t` from `'#/locales'` at module load time. Mock the locale module so importing the file does not pull in the entire i18n runtime:

```ts
import type { SystemProductApi } from "#/api/system/product";

import { describe, expect, it, vi } from "vitest";

vi.mock("#/locales", () => ({ $t: (key: string) => key }));

import { buildSomeTree, getTypeOptions } from "../data";

describe("buildSomeTree", () => {
  it("returns an empty array when input is empty", () => {
    expect(buildSomeTree([])).toEqual([]);
  });
  // ...covers filtering, sorting, recursion, edge cases
});
```

### API test pattern

Use `vi.hoisted` to share mock functions between the (hoisted) `vi.mock` factory and the test bodies 鈥?referencing outer variables in the factory directly will throw `Cannot access 'x' before initialization`.

```ts
import { beforeEach, describe, expect, it, vi } from "vitest";

const { get, post, put } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock("#/api/request", () => ({
  requestClient: { get, post, put },
}));

import { createProduct, getProductList } from "#/api/system/product";

beforeEach(() => {
  get.mockReset();
  post.mockReset();
  put.mockReset();
});

describe("SystemProduct API", () => {
  it("getProductList passes pagination params to GET /product", async () => {
    get.mockResolvedValue({ items: [], total: 0 });
    await getProductList({ pageIndex: 1, pageSize: 10 });
    expect(get).toHaveBeenCalledWith("/product", {
      params: { pageIndex: 1, pageSize: 10 },
    });
  });

  it("createProduct posts the payload to /product", async () => {
    post.mockResolvedValue({});
    await createProduct({ name: "A", price: 1 });
    expect(post).toHaveBeenCalledWith("/product", { name: "A", price: 1 });
  });
});
```

> The root `vitest.config.ts` already aliases `#` 鈫?`apps/web-antd/src`, so `#/...` imports work in tests.

### Test conventions

- Use **lowercase** `describe` titles (rule `test/prefer-lowercase-title`).
- Place all imports at the **top** of the file. The `vi.mock` call is hoisted automatically 鈥?it does **not** count as code "before" the import (rule `import/first`).
- One `__tests__/` folder per source folder; one `*.test.ts` file per source file.

---

## Step 16 鈥?Verification: Lint, Type Check, Tests

Before considering the feature complete, run the full pipeline locally and ensure **all three** exit with code 0. CI runs the same commands on every PR 鈥?fixing failures locally is much faster.

```pwsh
# from repo root (or src/web)
cd src/web

pnpm format       # auto-fix prettier issues (optional but recommended)
pnpm lint         # prettier --check + eslint
pnpm check:type   # vue-tsc across the monorepo
pnpm test:unit    # vitest run --dom (includes new tests added in Step 15)
```

Common failures and the fix:

| Failure                                                                 | Fix                                                                              |
| ------------------------------------------------------------------------ | ----------------------------------------------------------------------------------- |
| Prettier reports "Code style issues found"                              | Run `pnpm format`                                                                |
| ESLint `unicorn/no-array-sort`                                          | Replace `.sort(...)` with `.toSorted(...)`                                       |
| ESLint `@typescript-eslint/no-non-null-assertion`                       | Remove `!`; use a null guard or `?.`                                             |
| ESLint `unicorn/no-await-expression-member`                             | Assign the awaited value first, then access members                              |
| TS "Object is possibly 'null'" on template refs                         | Null-check the value (`?.` or `if (!ref) return;`) before using `getEl()` etc.   |
| TS "Argument of type 'Record<string, any>' is not assignable" in slots  | Cast the slot scope variable (e.g. `record as SystemProductApi.Product`)         |
| Vitest: `Cannot access 'x' before initialization` inside a mock factory | Wrap shared mocks in `vi.hoisted(() => ({ ... }))`                               |
| Vitest: cannot resolve `#/...`                                          | Confirm the `#` alias is present in [vitest.config.ts](../src/web/vitest.config.ts) |

> **Backend mirror**: run `dotnet build` and `dotnet test` from `src/api` before committing if any backend file changed.

### Backend coverage requirement (鈮?0% on new code)

Every backend change must keep new-code unit-test line coverage at **90% or above**. SonarCloud enforces this via the `sonar.coverage.exclusions` list in .github/workflows/gate.yml, which already excludes pure boilerplate that isn't worth unit-testing:

- `Migrations/**`, `Program.cs`
- DI/bootstrap files: `Extensions/InjectionExtension.cs`, `OptionsExtension.cs`, `MapsterExtension.cs`, `ServicesExtension.cs`
- POCO-only folders: `Entity/**`, `Model/**`, `Constants/**`, `Options/**`, `Validations/**`, `Exceptions/**`

Everything else (Controllers, Services, Attributes, Infrastructure, non-DI Extensions) is in the denominator and **must** be tested.

Run coverage locally:

```pwsh
cd src/api
dotnet test JfYu.WebApi.Template.slnx --collect:"XPlat Code Coverage" `
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

Test patterns to copy when adding tests for the listed components:

| Component type                                                   | Setup pattern                                                                                                                                                                                                         |
| ------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Service (DB-backed)                                              | `DbContextFactory.CreateInMemory()` returns `(AppDbContext, ReadonlyDBContext<AppDbContext>)`; configure Mapster `TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true).IgnoreNullValues(true)`            |
| Controller                                                       | Named-mock factory `CreateController()` returning the controller plus its mocked dependencies                                                                                                                         |
| MVC filter / `IAuthorizationFilter` (e.g. `PermissionAttribute`) | Build `AuthorizationFilterContext` from `DefaultHttpContext` + `RouteData` + `ActionDescriptor { EndpointMetadata = [...] }`; populate `HttpContext.RequestServices` via `ServiceCollection().BuildServiceProvider()` |
| Middleware (e.g. `BlacklistMiddleware`)                          | `DefaultHttpContext` with `User`/`Endpoint`; spy `RequestDelegate next`; mock `IRedisService`/`IDatabase`; register `IOptions<JsonOptions>` in `RequestServices`                                                      |
| EF interceptor (e.g. `AuditInterceptor`)                         | `DbContextFactory.CreateInMemory()` registering the interceptor in `optionsBuilder.AddInterceptors(...)`; assert via reading the AuditLog table                                                                       |

---

## Completion Checklist

### Backend

- [ ] DB fields reviewed and approved by user
- [ ] Entity created / modified, registered in `AppDbContext`
- [ ] Migration command reminder given
- [ ] `Create<Name>Request`, `Update<Name>Request`, `<Name>Response` created in `Model/<Name>/`
- [ ] FluentValidation validators created
- [ ] Error codes added to `ErrorCode.cs` in the correct range/region
- [ ] Options class + `OptionsExtension` registration added (if needed)
- [ ] Service interface + implementation created
- [ ] DI registered in `InjectionExtension`
- [ ] Unit tests added for the new Service / Controller / Validators / filters / middleware (matching the patterns table); **new-code coverage 鈮?0%** when measured with `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Permission codes added to `PermissionCodes.cs` (module + actions), parent confirmed with user
- [ ] Controller created, inheriting `CustomController`, with `[Permission]` on class and all actions

### Frontend

- [ ] All new files live under `src/web/apps/web-antd/src/` 鈥?**no edits to** `src/web/packages/**`, `src/web/internal/**`, `src/web/scripts/**`, or `apps/web-antd/src/{main.ts,bootstrap.ts,preferences.ts,layouts/**}` (Vben framework code; see [src/web/AGENTS.md](../src/web/AGENTS.md) 搂"Modification Boundaries")
- [ ] Router entry added (new file or child route in existing domain file)
- [ ] API file created/extended in `src/api/system/`, exported from `src/api/system/index.ts`
- [ ] `data.ts` created with `useFormSchema`, `useGridFormSchema`, `useColumns`
- [ ] `modules/form.vue` created (drawer create/edit form) 鈥?CRUD only
- [ ] `index.vue` created (list page with VxeGrid using correct module type pattern)
- [ ] Error messages added to the backend `Resources/EnumMessages.resx` (English) and `EnumMessages.zh-CN.resx` (Chinese) 鈥?no frontend `error.json`
- [ ] `system.json` updated in both `zh-CN` and `en-US` with all UI labels
- [ ] `page.json` updated in both locales if a new route/menu title was added
- [ ] Unit tests added under `__tests__/` for `data.ts` pure logic and the new API module (Step 15)

### Verification (run before declaring done)

- [ ] `pnpm lint` exits 0 in `src/web` (run `pnpm format` first if prettier complains)
- [ ] `pnpm check:type` exits 0 in `src/web`
- [ ] `pnpm test:unit` exits 0 in `src/web` (new tests are listed and passing)
- [ ] `dotnet build` and `dotnet test` succeed from `src/api` (backend changes only)
- [ ] Backend new-code unit-test coverage is **鈮?0%** (verify via SonarCloud PR decoration or `dotnet test --collect:"XPlat Code Coverage"`); boilerplate already excluded via `sonar.coverage.exclusions` in .github/workflows/gate.yml

