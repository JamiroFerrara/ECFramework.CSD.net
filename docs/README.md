# ECFramework.CSD.net Documentation

Allitude's universal generic entity framework. Provides reusable CRUD APIs, permission handling, and strongly-typed TypeScript client generation via Swagger.

## What It Does

Creates **automatic, type-safe CRUD APIs** from C# entities using `EntityController<T>`. Backend publishes Swagger spec → frontend generates TypeScript client with zero config.

### Core Features

- **Generic EntityController**: Auto CRUD (Create, Read, Update, Delete, LogicalDelete)
- **Swagger Integration**: Express OpenAPI spec → typed REST contracts
- **TypeScript Client Generation**: swagger-typescript-api auto-generates `client.*` namespace
- **Permission Enforcement**: `[CSDPermissions(...)]` per-controller access control
- **DbContext Injection**: EF Core + Dapper coexist on same connection
- **Async-first**: Non-blocking Task-based operations throughout
- **Soft Deletes**: `ISoftDeletable` interface for logical deletes

## Quick Start

### Backend (C#)

```cs
// 1. Define entity
public class PMS_DORA
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
    // ...
}

// 2. Create partial controller in Api/Index.cs
public partial class Dora : EntityController<PMS_DORA>
{
    public Dora(DbContext context, IConfiguration config) 
        : base(context, config) { }
}

// 3. (Optional) Extend with custom logic in Api/Dora/CustomMethod.cs
public partial class Dora
{
    [HttpGet("CustomEndpoint")]
    public async Task<Response<PMS_DORA>> CustomMethod(...)
    {
        return await base.GetPage(req, query => 
        {
            // Custom filtering
            return query.Where(x => x.Active);
        });
    }
}
```

### Frontend (TypeScript/React)

```typescript
import client from 'services/client/request';
import { useQuery } from 'utils/hooks/useQuery';

// Type-safe API calls
const api = useQuery(client.dora.getPage, { 
    Like: { Name: 'Production' },
    PageSize: 12
});

// Reactive queries
api.query({ Like: { Name: 'Staging' } });

// Render with response
{api.state?.items?.map(item => (
    <div key={item.Id}>{item.Name}</div>
))}
```

## Documentation Index

- **[Architecture](./01-architecture.md)** – How components fit together
- **[EntityController](./02-entity-controller.md)** – Generic CRUD base class, methods, patterns
- **[Request/Response Models](./03-request-response.md)** – Standard DTO structure, filtering, pagination
- **[Permissions](./04-permissions.md)** – Access control via `[CSDPermissions(...)]`
- **[TypeScript Client](./05-typescript-client.md)** – Generation, usage in React, hooks
- **[Frontend Patterns](./06-frontend-patterns.md)** – useQuery, form binding, real-world examples
- **[Backend Extensions](./07-backend-extensions.md)** – Custom endpoints, partial classes, filtering
- **[Database Patterns](./08-database-patterns.md)** – EF Core, soft deletes, ISoftDeletable
- **[Joins & Relationships](./09-joins-and-relationships.md)** – Include patterns, filtered joins, many-to-many

## File Structure

```
ECFramework.CSD.net/
├── Attributes/           # Permission attributes, filters
├── Extensions/           # EF, Dapper, response helpers
├── Interfaces/           # IKeyable, IModifiable, IModuser
├── Models/              # Generic Request<T>, Response<T>, CSDContext
├── Parser/              # Filter expression parsing
├── EntityController.cs  # Generic CRUD base
├── Entity.cs            # Base entity class
└── Exceptions.cs        # Framework exceptions
```

## Integration Points

### With allitude-pms-service

- Controllers inherit `EntityController<T>` from this framework
- `Api/Index.cs` auto-registers partial controller classes
- Custom endpoints override base methods with `base.GetPage(req, actionFunc)`

### With allitude-pms-fe

- Swagger spec generated from service
- `npm run build` (during postinstall) runs `swagger-typescript-api` 
- Generated client at `src/client/client.ts`
- React hooks wrap client methods (useQuery, etc.)

## Key Concepts

| Concept | Purpose | Example |
|---------|---------|---------|
| **Request<T>** | API input DTO | `{ Like: { Name: "test" }, PageSize: 10 }` |
| **Response<T>** | API output DTO | `{ items: [...], totalPages: 5, canRead: true }` |
| **IKeyable** | Entity with primary key | `GetItem([FromBody] Request<E>)` finds by key |
| **ISoftDeletable** | Logical delete support | `LogicalDelete` sets `DeletedAt` instead of removing |
| **CSDContext** | Request context (user, app, device) | Injected via `[RequestHydrationFilter]` |
| **Partial Classes** | Controller extension points | Separate logic per domain entity |

## Next Steps

- Read **[Architecture](./01-architecture.md)** to understand data flow
- Check **[Backend Extensions](./07-backend-extensions.md)** to add custom endpoints
- See **[Frontend Patterns](./06-frontend-patterns.md)** for React integration
