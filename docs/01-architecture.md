# Architecture

How ECFramework connects C# entities to type-safe TypeScript client methods.

## Request/Response Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      Frontend (React)                           │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ useQuery(client.dora.getPage, params)                   │   │
│  │  ↓ (reactive params change)                             │   │
│  │ axios({ method: 'GET', url: '/api/Dora/GetPage', ...}) │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────┬──────────────────────────────────────────┘
                       │ JSON Request<T>
                       ↓
┌──────────────────────────────────────────────────────────────────┐
│              Backend (C# / ASP.NET Core)                         │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ [Route("api/[controller]")]                              │   │
│  │ public class Dora : EntityController<PMS_DORA>           │   │
│  │ {                                                        │   │
│  │   [HttpGet("GetPage")]                                  │   │
│  │   public async Task<Response<T>> GetPage(                │   │
│  │     [FromBody] Request<PMS_DORA> req                    │   │
│  │   ) => await base.GetPage(req, query => query);         │   │
│  │ }                                                        │   │
│  │                                                          │   │
│  │ // EntityController<T> internals:                        │   │
│  │ // 1. Parse request filters (Like, Expressions)         │   │
│  │ // 2. Apply IQueryable transformations                  │   │
│  │ // 3. Check permissions ([CSDPermissions])              │   │
│  │ // 4. Execute async query                               │   │
│  │ // 5. Return typed Response<T>                          │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────┬──────────────────────────────────────────┘
                       │ JSON Response<T>
                       ↓
┌──────────────────────────────────────────────────────────────────┐
│                      Frontend (React)                            │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ useQuery setState(response)                              │   │
│  │ api.state = { items: [...], totalPages: 5, ... }        │   │
│  │ Re-render with typed data                                │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

## Code Generation Pipeline

```
                    Backend
                  (C# Service)
                       │
                       ↓
              ┌────────────────┐
              │ Swagger / API   │
              │ Specifications  │
              │ (route + types) │
              └────────────────┘
                       │
                       │ [npm postinstall]
                       ↓
              ┌────────────────┐
              │   swagger-     │
              │ typescript-api │
              │   Generator    │
              └────────────────┘
                       │
                       ↓
    ┌──────────────────────────────────┐
    │  TypeScript Client Types & API   │
    │  src/client/client.ts            │
    │                                  │
    │  export class Api {              │
    │    dora = {                       │
    │      getPage: (...) => Promise   │
    │      create: (...) => Promise    │
    │      update: (...) => Promise    │
    │      delete: (...) => Promise    │
    │    };                            │
    │  }                               │
    └──────────────────────────────────┘
           │
           │ [Used in React]
           ↓
    ┌──────────────────────────────────┐
    │  useQuery Hook                   │
    │  const api = useQuery(           │
    │    client.dora.getPage,          │
    │    { Like: {...} }               │
    │  );                              │
    │                                  │
    │  // Fully type-checked!          │
    └──────────────────────────────────┘
```

## Generic EntityController<T>

The backbone of the framework. Provides auto CRUD methods:

### Core Methods

| Method | Purpose | Signature | Returns |
|--------|---------|-----------|---------|
| `GetItem` | Single entity by key | `Request<E> → IQueryable<E> → E` | `Response<E>` with 1 item |
| `GetPage` | Paginated list | `Request<E> → IQueryable<E> → List<E>` | `Response<E>` with page metadata |
| `GetExcel` | Bulk export | `Request<E> → byte[]` | Excel file in `Response.file` |
| `Create` | Insert batch | `List<E> → Response<E>` | Created items |
| `Update` | Modify existing | `Request<E> → IQueryable<E> → E` | Updated item |
| `Delete` | Remove hard | `Request<E> → IQueryable<E> → void` | Deleted item list |
| `LogicalDelete` | Soft delete | `Request<E> → ISoftDeletable` | Item with `DeletedAt` set |

### Generic Action Injection

All CRUD methods accept an `action: Func<IQueryable<E>, IQueryable<E>>` for custom filtering:

```cs
// Base GetPage with default action
await base.GetPage(req, query => query);

// Custom filtering action
await base.GetPage(req, query =>
{
    // Custom business logic
    return query.Where(x => x.Active)
                .Where(x => x.OwnerId == userId);
});
```

## Request/Response Types

### Request<T>

Generic input DTO. Standardizes filtering, pagination, ordering:

```cs
public class Request<T>
{
    public CSDContext ctx { get; set; }               // User, app, device
    public Dictionary<string, object> Like { get; set; } // Loose matching
    public Dictionary<string, object> Expressions { get; set; } // Full LINQ
    public int Page { get; set; }                      // 0-indexed
    public int? PageSize { get; set; }                 // Default: 12
    public string OrderBy { get; set; }                // "FieldName" or "FieldName desc"
    public StringStringValueTuple[] Schema { get; set; } // For Excel column config
    public T Item { get; set; }                        // Single item update/delete
    public List<T> Items { get; set; }                 // Batch operations
}
```

### Response<T>

Generic output DTO. Wraps results + metadata:

```cs
public class Response<T>
{
    public string ErrorGuid { get; set; }              // Error tracing
    public int Rc { get; set; }                        // Return code
    public string RcDescription { get; set; }          // User-friendly error
    public CSDTimer[] RcTimers { get; set; }           // Perf metrics
    public List<T> items { get; set; }                 // Result list
    public T item { get; set; }                        // Single result
    public int totalPages { get; set; }                // Pagination
    public int totalItems { get; set; }                // Total count
    public byte[] file { get; set; }                   // Excel export
    public string fileName { get; set; }               // Filename hint
    public bool canRead { get; set; }                  // Permission check
    public bool canWrite { get; set; }                 // Permission check
}
```

## Permission Model

Permissions enforced at controller level via `[CSDPermissions]`:

```cs
[CSDPermissions(["PMS-R", "PMS-W"])]
public class MacroDati : EntityController<PMS_MACRODATI> { ... }
```

- **1st element**: Read permission key (for GetItem, GetPage, GetExcel)
- **2nd element**: Write permission key (for Create, Update, Delete, LogicalDelete)

Framework checks against `CSDContext.User` permissions before executing method.

## Entity Traits

### IKeyable

Mark entities with database keys:

```cs
public interface IKeyable { }

public class PMS_DORA : IKeyable
{
    [Key]
    public string Id { get; set; }
}
```

Allows `GetItem` to find by primary key automatically.

### ISoftDeletable

Enable logical deletes:

```cs
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
    bool IsDeleted { get; set; }
}

public class PMS_MACRODATI : ISoftDeletable
{
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

`LogicalDelete` method checks this interface; if present, sets `DeletedAt` and `IsDeleted = true` instead of hard delete.

## Axios Interceptor Chain

Frontend wraps axios with Allitude context injection via `src/services/client/interceptorFunctions.ts`:

```typescript
// Every request automatically includes:
// 1. CSDContext (user token, ABI codes, holding info)
// 2. Authorization headers
// 3. Error handling & retry logic

axios.interceptors.request.use((config) => {
    config.headers.Authorization = `Bearer ${token}`;
    config.data.ctx = { 
        user: { CodiceToken: token, CodiceAbiDefault: abi },
        application: { ... },
        device: { ... }
    };
    return config;
});
```

## Extension Points

### Backend

- **Partial Classes**: Split controller logic across files
- **Action Injection**: Custom IQueryable transformations in CRUD methods
- **Override Methods**: Redefine GetPage, Create, etc. with custom behavior
- **Custom Endpoints**: Add non-CRUD `[HttpGet/Post/Patch/Delete]` methods

### Frontend

- **useQuery Hook**: Wraps client promises, handles loading/pagination state
- **Form Binding**: Formik integration via Jormik for type-driven UI
- **Interceptors**: Request/response middleware for auth, error handling

## Summary

1. **Define C# entity** with interfaces (IKeyable, ISoftDeletable)
2. **Create partial controller** inheriting EntityController<T>
3. **Service publishes Swagger spec** including all routes + types
4. **Frontend runs `swagger-typescript-api`** → generates `client.*` namespace
5. **React calls `useQuery(client.dora.getPage, params)`** → fully typed
6. **Response auto-typed** with permission flags + pagination metadata
