# ECFramework.CSD.net — Complete Documentation Index

Full guide to building type-safe CRUD APIs with automatic TypeScript client generation.

## Quick Navigation

### For Backend Developers (C#)

1. **[Architecture](./01-architecture.md)** – Understand the framework holistically
   - Request/response flow diagram
   - Code generation pipeline
   - Generic EntityController<T> internals
   - Permission model
   - Extension points

2. **[EntityController](./02-entity-controller.md)** – Reference for all CRUD methods
   - GetItem, GetPage, GetExcel
   - Create, Update, Delete, LogicalDelete
   - Internal helpers (ApplyWhere, ApplyIncludes, ApplyOrderBy)
   - Permission enforcement
   - Injectable hooks

3. **[Backend Extensions](./07-backend-extensions.md)** – Add custom endpoints
   - Partial controller pattern
   - Override base methods with custom actions
   - Complex filtering examples (real pms-service code)
   - Bulk operations, aggregations, exports
   - Custom response types

4. **[Database Patterns](./08-database-patterns.md)** – Entity modeling, relationships, soft deletes
   - DbContext setup
   - IKeyable, ISoftDeletable, IModifiable interfaces
   - One-to-many, many-to-many, composite keys
   - Query filters, indexes, migrations
   - Dapper integration

5. **[Joins & Relationships](./09-joins-and-relationships.md)** – Include patterns, filtered joins
   - Automatic eager-loading via reflection
   - Manual includes in custom endpoints
   - Filtered joins with Where().Any()
   - Multi-level includes (ThenInclude)
   - Performance optimization

### For Frontend Developers (TypeScript/React)

1. **[Architecture](./01-architecture.md)** – See how client is generated
   - Code generation pipeline (Swagger → swagger-typescript-api)
   - Request/response flow
   - Generic Request<T>, Response<T> types

2. **[TypeScript Client](./05-typescript-client.md)** – Generated API reference + usage
   - Generation setup (one-time, postinstall)
   - Generated structure (Api class, entity types, HttpClient)
   - Basic usage (frontend setup)
   - CRUD operations (create, read, update, delete)
   - Excel export
   - Error handling, interceptors

3. **[Frontend Patterns](./06-frontend-patterns.md)** – Real React examples (from allitude-pms-fe)
   - useQuery hook (reactive, paginated queries)
   - Data table + SemanticTableGrid
   - Filter forms with Formik
   - Detail page (fetch single, edit, save)
   - Create new item
   - Delete with confirmation modal
   - Jormik (type-driven form generation)
   - usePromise (single-shot queries)
   - Permission-based UI

### For Both

1. **[Request/Response Models](./03-request-response.md)** – Standard DTOs
   - Request<T> fields: filtering, pagination, sorting
   - Response<T> fields: results, metadata, permissions, errors
   - CSDContext (user, app, device info)
   - Common response codes

2. **[Permissions](./04-permissions.md)** – Access control
   - [CSDPermissions] attribute
   - Framework permission enforcement
   - Frontend permission checks
   - Permission bypass (dev only)
   - Custom permission logic
   - Testing permissions

---

## Learning Path

### Beginner

1. Read **[Architecture](./01-architecture.md)** (15 min)
   - Understand request/response flow
   - See code generation pipeline

2. Read **[Frontend Patterns](./06-frontend-patterns.md)** – useQuery section (15 min)
   - Basic API calls from React
   - State management pattern

3. Try example: Simple list page + pagination

### Intermediate

1. Read **[EntityController](./02-entity-controller.md)** – Methods section (20 min)
   - Each CRUD method: signature, behavior, usage

2. Read **[Request/Response Models](./03-request-response.md)** (15 min)
   - Understand filtering patterns (Like vs Expressions)
   - Pagination, sorting

3. Read **[Frontend Patterns](./06-frontend-patterns.md)** – remaining sections (30 min)
   - Data table, filters, detail pages
   - Create/delete workflows

4. Try examples:
   - Build filter form that reactively queries
   - Detail page with fetch + edit + save
   - Delete with confirmation

### Advanced

1. Read **[Backend Extensions](./07-backend-extensions.md)** (30 min)
   - Partial controller pattern
   - Custom actions (complex filtering)
   - Bulk ops, aggregations, exports

2. Read **[Database Patterns](./08-database-patterns.md)** (30 min)
   - Entity relationships, soft deletes
   - Query filters, performance indexes

3. Read **[Joins & Relationships](./09-joins-and-relationships.md)** (20 min)
   - Automatic & manual includes
   - Filtered joins (Where().Any())
   - Performance optimization

4. Read **[Permissions](./04-permissions.md)** (15 min)
   - Custom permission logic

5. Try examples:
   - Add custom GET endpoint with complex filtering + joins
   - Add bulk import with Dapper
   - Implement filtered join (association checks)
   - Implement hierarchical permissions
   - Add soft-delete with query filter

---

## Real-World Examples

All code examples reference actual implementations:

### From **allitude-pms-service** (Backend)
- `Api/Index.cs` – Controller registration (how to instantiate)
- `Api/MacroDati/GetRelatedPage.cs` – Custom filtering action (complex example)
- `Api/Pms/CreatePms.cs` – Custom response type with multiple DTOs
- Entity definitions: `PMS_MACRODATI`, `PMS_DORA`, `CLA_TO_MACRODATI`
- Permission pattern: `[CSDPermissions(["PMS-READ", "PMS-WRITE"])]`

### From **allitude-pms-fe** (Frontend)
- `src/services/client/request.ts` – API instance creation
- `src/utils/hooks/useQuery.tsx` – useQuery implementation (reactive, paginated)
- `src/pages/Macrodati/index.tsx` – List page + filter + table + pagination
- `src/pages/Macrodati/detail.tsx` – Detail page + Jormik form
- `src/components/Filters/Filters.tsx` – Reusable filter form component
- `src/client/client.ts` – Generated types (auto-created from Swagger)

---

## Conceptual Hierarchy

```
ECFramework.CSD.net
│
├─ EntityController<T>  [Abstract generic base]
│  ├─ GetItem()
│  ├─ GetPage()
│  ├─ Create()
│  ├─ Update()
│  ├─ Delete()
│  └─ LogicalDelete()
│
├─ Request<T>  [Input DTO]
│  ├─ Like: filtering (loose)
│  ├─ Expressions: filtering (strict)
│  ├─ Page, PageSize: pagination
│  └─ OrderBy: sorting
│
├─ Response<T>  [Output DTO]
│  ├─ items/item: results
│  ├─ totalPages/totalItems: pagination metadata
│  ├─ canRead/canWrite: permissions
│  └─ Rc/RcDescription: error codes
│
├─ CSDContext  [Request context]
│  ├─ user: CodiceToken, CodiceAbiDefault, ProfiloUtente, etc.
│  ├─ application: CodApplicazione, Environment
│  └─ device: Type, Modello
│
├─ [CSDPermissions]  [Access control]
│  ├─ Read permission (1st element)
│  └─ Write permission (2nd element)
│
├─ ISoftDeletable  [Logical delete support]
│  ├─ DeletedAt: DateTime?
│  └─ IsDeleted: bool
│
├─ Include() / ThenInclude()  [Join mechanism]
│  ├─ .Include(x => x.Related)
│  ├─ .ThenInclude(x => x.NestedRelated)
│  └─ .Where(x => x.Related.Any())
│
└─ Swagger → swagger-typescript-api  [Code generation]
   ├─ Generates Api class with all routes
   ├─ Generates Request<T>, Response<T> types
   └─ Generates entity interface types
```

---

## Common Tasks

### "I want to..."

**...add a new CRUD API**
1. Create entity class (implement IKeyable, ISoftDeletable if needed)
2. Add DbSet to DbContext
3. Add partial controller in Api/Index.cs
4. (Optional) Override methods or add custom endpoints
5. Run backend (Swagger auto-generated)
6. Frontend runs `swagger-typescript-api` (auto on postinstall)
7. Use `client.myEntity.getPage()` etc in React

**...add custom filtering to a CRUD method**
1. Create `Api/MyEntity/GetPage.cs` partial class
2. Override GetPage, call `base.GetPage(req, query => { /* custom filter */ })`
3. Push to frontend (regenerates client automatically)

**...restrict access by permission**
1. Add `[CSDPermissions(["READ-KEY", "WRITE-KEY"])]` to controller
2. Frontend checks `response.canRead`, `response.canWrite`
3. Show/hide buttons based on flags

**...export data as Excel**
1. Call GetExcel endpoint: `client.myEntity.getExcel({...})`
2. Download response.file with response.fileName

**...soft-delete instead of hard-delete**
1. Implement ISoftDeletable on entity
2. Call LogicalDelete endpoint
3. Query filter automatically hides deleted items

**...implement bulk import**
1. Create custom endpoint in partial controller
2. Use Dapper for bulk insert (faster than EF)
3. Return Response<T> with import results

**...filter by related data (joins)**
1. Override GetPage with `base.GetPage(req, query => query.Where(x => x.Related.Any(...)))`
2. Use `.Where().Any()` for existence checks (no explicit Join needed)
3. Use `.Include().ThenInclude()` in custom action if need to return related data

**...control which relationships get included**
1. Default: `ApplyIncludes()` auto-loads all navigation properties
2. Custom: Override method with selective `.Include()` calls
3. Lite endpoint: Include only critical relationships (performance)

**...add complex business logic to CRUD**
1. Create partial controller file with custom endpoint
2. Use base methods if possible (reuse permission checks)
3. Or implement custom logic with Try<Response<T>>() wrapper

---

## File Structure Reference

```
ECFramework.CSD.net/
├── docs/
│   ├── 00-INDEX.md                  ← You are here
│   ├── README.md
│   ├── EXAMPLES.md
│   ├── 01-architecture.md
│   ├── 02-entity-controller.md
│   ├── 03-request-response.md
│   ├── 04-permissions.md
│   ├── 05-typescript-client.md
│   ├── 06-frontend-patterns.md
│   ├── 07-backend-extensions.md
│   ├── 08-database-patterns.md
│   └── 09-joins-and-relationships.md
│
├── Attributes/                      # Permission filters
│   ├── CSDPermissions.cs
│   └── RequestHydrationFilter.cs
│
├── Extensions/                      # Helper methods
│   ├── DbContextExtensions.cs
│   ├── DapperExtensions.cs
│   ├── ExcelExtensions.cs
│   └── ResponseExtensions.cs
│
├── Interfaces/
│   ├── IKeyable.cs                 # Entity with primary key
│   ├── ISoftDeletable.cs           # Support logical delete
│   ├── IModifiable.cs              # Track ModDate, ModUser
│   └── IModuser.cs
│
├── Models/
│   ├── Request.cs                  # Generic input DTO
│   ├── Response.cs                 # Generic output DTO
│   ├── CSDContext.cs               # Request context
│   ├── CSDParam.cs
│   ├── CSDTimer.cs
│   └── ...
│
├── Parser/                          # Filter expression parsing
│   ├── ExpressionParser.cs
│   └── FilterExpression.cs
│
├── EntityController.cs              # Generic CRUD base (THE core)
├── Entity.cs                        # Base entity class
├── Exceptions.cs                    # Custom exceptions
└── EntityMethods.cs                 # Helper methods
```

---

## Integration Points

### With allitude-pms-service

- Services implement `EntityController<T>` (e.g., `Dora : EntityController<PMS_DORA>`)
- `Api/Index.cs` registers partial controllers
- Custom logic in `Api/[Entity]/[Method].cs` files
- Swagger spec published at `/swagger/index.html`

### With allitude-pms-fe

- `src/client/client.ts` auto-generated from backend Swagger spec
- `useQuery()` hook wraps client promises
- `Jormik` component for declarative form generation from types
- Interceptors inject CSDContext on every request

---

## Key Takeaways

1. **One source of truth**: Backend entity definitions → Swagger spec → TypeScript types
   - Change C# entity → Swagger updates → Client regenerates → Frontend has latest types

2. **Async throughout**: All CRUD methods are Task-based, non-blocking

3. **Permission enforcement at framework level**: Every CRUD method checks `[CSDPermissions]`

4. **Soft deletes by default**: ISoftDeletable + query filters hide deleted items transparently

5. **Flexible filtering**: Like (loose) + Expressions (strict) + custom action injection

6. **Frontend safety**: Generated types prevent TypeScript errors, response flags show permissions

7. **Extension at multiple levels**:
   - Backend: partial classes + action injection
   - Frontend: custom hooks, interceptors, form components

---

## Glossary

| Term | Meaning |
|------|---------|
| **EntityController<T>** | Generic async CRUD base for entity T |
| **Request<T>** | Standardized input DTO (filters, pagination, context) |
| **Response<T>** | Standardized output DTO (results, metadata, permissions) |
| **CSDContext** | Request context (user, app, device) auto-injected by framework |
| **IKeyable** | Entity interface marking that entity has primary key |
| **ISoftDeletable** | Entity interface enabling LogicalDelete (marks as deleted, doesn't remove) |
| **[CSDPermissions]** | Controller attribute defining read/write permission keys |
| **Try<T>** | Permission check wrapper; returns Response<T> with canRead/canWrite flags |
| **Partial class** | C# feature allowing splitting controller logic across multiple files |
| **Injectables** | Static class with lifecycle hooks (RunCreate, RunUpdate, RunDelete, etc.) |
| **swagger-typescript-api** | Code generator: Swagger spec → TypeScript client with types |
| **useQuery** | React hook wrapping client.X.getPage() with reactive params + pagination state |
| **Jormik** | Declarative form component: types → validation → UI components |
| **Soft delete** | Mark as deleted (set DeletedAt, IsDeleted) without removing from DB |
| **Hard delete** | Remove permanently from DB (use Delete method) |
| **Like** | Loose filter: substring matching (WHERE col LIKE '%value%') |
| **Expressions** | Strict filter: exact matching (WHERE col = value) |
| **Include** | EF eager-load: fetch related entity in same query (not separate) |
| **ThenInclude** | Multi-level include: traverse nested relationships |
| **Where().Any()** | Filter by relationship existence (generates EXISTS subquery) |
| **ApplyIncludes** | Framework method: auto-includes all navigation properties via reflection |
| **Navigation Property** | Virtual property linking to related entity/collection |
| **N+1 Query Problem** | Loading parent → then N separate queries for children (framework avoids via auto-include) |

---

## Next Steps

1. **Start here**: [Architecture](./01-architecture.md) (15 min overview)
2. **Pick a path**:
   - **Backend**: [EntityController](./02-entity-controller.md) → [Extensions](./07-backend-extensions.md) → [Database](./08-database-patterns.md)
   - **Frontend**: [TypeScript Client](./05-typescript-client.md) → [Frontend Patterns](./06-frontend-patterns.md)
   - **Both**: [Request/Response](./03-request-response.md) + [Permissions](./04-permissions.md)
3. **Build**: Create simple list → filter form → detail page → delete modal
4. **Extend**: Add custom endpoint → complex filtering → soft deletes → aggregations
