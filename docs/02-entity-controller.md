# EntityController<T>

Generic async CRUD base for any entity T.

## Instantiation

Controllers inherit from `EntityController<T>` where T must be a POCO (plain C# object):

```cs
public partial class Dora : EntityController<PMS_DORA>
{
    public Dora(DbContext context, IConfiguration configuration)
        : base(context, configuration)
    {
        this.ctx = context;  // Store for use in partial overrides
    }
}
```

Registered in `Api/Index.cs`:

```cs
public partial class Dora : EntityController<PMS_DORA>
{
    public new DbContext ctx;
    public Dora(DbContext context, IConfiguration configuration)
        : base(context, configuration)
    {
        this.ctx = context;
    }
}
```

## Methods

### GetItem

Fetch single entity by primary key.

**Signature**
```cs
public async Task<Response<E>> GetItem(
    [FromBody] Request<E> req,
    Func<DbSet<E>, DbSet<E>> action
)
```

**What it does**

1. Gets `DbSet<E>` from context
2. Applies Where filters from `Request.Expressions` if present
3. Applies Include relationships via `ApplyIncludes`
4. Applies OrderBy sorting
5. Executes `FirstOrDefaultAsync()` (no tracking)
6. Runs injectable `Injectables.RunGetItem(item, this)`
7. Populates `canRead`, `canWrite` from permission check
8. Returns `Response<E>` with single `item`

**Usage**

```typescript
// Frontend
const singleItem = await client.dora.getItem({ 
    Like: { Id: '12345' } 
});
```

```cs
// Backend override
[HttpGet("GetItem")]
public async Task<Response<PMS_DORA>> GetItem([FromBody] Request<PMS_DORA> req) =>
    await base.GetItem(req, query => query);
```

---

### GetPage

Fetch paginated list of entities.

**Signature**
```cs
public async Task<Response<E>> GetPage<R>(
    Request<R> req,
    Func<IQueryable<E>, IQueryable<E>> action
) where R : class, new()
```

**What it does**

1. Gets `DbSet<E>` as `IQueryable`
2. Sets default `PageSize = 12` if null
3. Applies Where filters from `Request.Expressions`
4. Applies Include relationships
5. Applies OrderBy sorting
6. Calls `action(query)` for custom filtering
7. Applies Skip/Take for pagination
8. Executes `ToListAsync()` (no tracking)
9. Counts total via `query.Count()`
10. Calculates `totalPages` from `Math.Ceiling(count / pageSize)`
11. Returns `Response<E>` with `items`, `totalPages`, `totalItems`

**Usage**

```typescript
// Frontend - reactive pagination
const api = useQuery(client.dora.getPage, {
    PageSize: 10,
    Page: 0,
    Like: { Name: 'Active' },
    OrderBy: 'CreatedAt desc'
});

// Change page
api.query({}, 2);  // Go to page 2
```

```cs
// Backend - no custom filtering
[HttpGet("GetPage")]
public async Task<Response<PMS_DORA>> GetPage([FromBody] Request<PMS_DORA> req) =>
    await base.GetPage(req, query => query);

// Backend - with custom filter action
[HttpGet("GetPage")]
public async Task<Response<PMS_DORA>> GetPage([FromBody] Request<PMS_DORA> req) =>
    await base.GetPage(req, query =>
    {
        // Only active DORA
        return query.Where(x => x.Active);
    });
```

---

### GetExcel

Export filtered results as `.xlsx` file.

**Signature**
```cs
public async Task<Response<E>> GetExcel<R>(
    Request<R> req,
    Func<IQueryable<E>, IQueryable<E>> action
) where R : class, new()
```

**What it does**

1. Calls `GetPage` internally with `PageSize = 999999999` (fetch all)
2. Checks `canRead` permission; throws if denied
3. Calls `ToExcel(items, fileName, schema)` extension
4. Returns `Response<E>` with `file` (byte[]) and `fileName`

**Usage**

```typescript
// Frontend
const response = await client.dora.getExcel({
    Like: { Active: true },
    Schema: [
        { key: 'Id', value: 'ID' },
        { key: 'Name', value: 'Nome' }
    ]
});

// Download
downloadFile(response.data.file, response.data.fileName);
```

---

### Create

Insert one or more entities.

**Signature**
```cs
public async Task<Response<E>> Create(
    List<E> items,
    Request<E> req,
    Func<DbSet<E>, DbSet<E>> action
)
```

**What it does**

1. Checks reflective IDs via `CheckReflectiveId(items)` (prevents duplicates)
2. Gets `DbSet<E>`
3. Calls `action(dbSet)` for custom setup
4. Adds each item via `dbSet.Add(item)`
5. Runs injectable `Injectables.RunCreate(item, this)` for each
6. Calls `SaveChangesAsync()`
7. Returns `Response<E>` with created `items`

**Usage**

```typescript
// Frontend - single
const newDora = { Id: 'new-id', Name: 'Test DORA' };
const response = await client.dora.create([newDora]);

// Frontend - batch
const items = [
    { Id: '1', Name: 'DORA1' },
    { Id: '2', Name: 'DORA2' }
];
const response = await client.dora.create(items);
```

---

### Update

Modify single or multiple entities.

**Signature**
```cs
public async Task<Response<E>> Update(
    [FromBody] Request<E> req,
    Func<IQueryable<E>, IQueryable<E>> action
)
```

**What it does**

1. Gets `IQueryable<E>` and calls `action(query)`
2. If `req.Expressions` exist and `req.Items` is null:
   - Applies Where filters
   - Gets first matching item
3. If `res.item` not null:
   - Runs injectable `Injectables.RunUpdate(req.Item, this)`
   - Maps `req.Item` properties to DB item via `SetValues`
   - Calls `SaveChangesAsync()`
4. Returns `Response<E>` with updated `item`

**Usage**

```typescript
// Frontend - single item
const updated = { Id: '123', Name: 'Updated Name' };
const response = await client.dora.update({ Item: updated });

// Frontend - by filter
const response = await client.dora.update({
    Like: { Status: 'Draft' },
    Item: { Status: 'Published' }
});
```

---

### Delete

Hard delete entities.

**Signature**
```cs
public virtual async Task<Response<E>> Delete(
    [FromBody] Request<E> req,
    Func<IQueryable<E>, IQueryable<E>> action
)
```

**What it does**

1. Gets `IQueryable<E>` and calls `action(query)`
2. If `req.Expressions` exist and `req.Items` is null:
   - Applies Where filters
   - Gets matching items
   - Populates `req.Items`
3. For each item in `req.Items`:
   - Runs injectable `Injectables.RunDelete(item, this)`
   - Calls `dbSet.Remove(item)`
4. Calls `SaveChangesAsync()`
5. Returns `Response<E>` with deleted items list

**Usage**

```typescript
// Frontend - by ID
const response = await client.dora.delete({
    Items: [{ Id: '123' }]
});

// Frontend - by filter
const response = await client.dora.delete({
    Like: { Status: 'Draft' }
});
```

---

### LogicalDelete

Soft delete (mark deleted without removing).

**Signature**
```cs
public virtual async Task<Response<E>> LogicalDelete(
    [FromBody] Request<E> req,
    Func<IQueryable<E>, IQueryable<E>> action
)
```

**What it does**

1. Requires entity to implement `ISoftDeletable`
2. Gets `IQueryable<E>` and applies filters
3. Finds matching items
4. Runs injectable `Injectables.RunLogicalDelete(item, this)`
5. Sets `DeletedAt = DateTime.UtcNow` and `IsDeleted = true`
6. Calls `SaveChangesAsync()`
7. Returns `Response<E>` with marked items

**Usage**

```typescript
// Frontend
const response = await client.macroDati.logicalDelete({
    Like: { Id: '123' }
});
```

---

## Internal Helpers

### ApplyWhere

Converts `Request.Expressions` dict to EF Core LINQ Where clauses.

```cs
IQueryable<E> ApplyWhere(
    IQueryable<E> query,
    Dictionary<string, object> expressions,
    List<string> allowedFields = null,
    string fieldPrefix = ""
)
```

Supports:
- `Like: { Name: "test" }` → contains matching
- `Expressions: { Id: "123" }` → exact matching
- Nested field navigation: `{ "User.Name": "John" }`

### ApplyIncludes

Eager-loads related entities via EF Core.

```cs
IQueryable<E> ApplyIncludes(IQueryable<E> query, DbContext ctx)
```

Reflects entity properties, includes navigation properties automatically.

### ApplyOrderBy

Parses OrderBy string: `"FieldName"` or `"FieldName desc"`.

```cs
IQueryable<E> ApplyOrderBy(IQueryable<E> query, string orderBy)
```

---

## Permission Enforcement

All CRUD methods call `Try<Response<E>>` which:

1. Checks `[CSDPermissions]` attribute on controller
2. Extracts required permission from context
3. If user lacks permission, returns response with `canRead/canWrite = false`
4. Frontend can show/hide buttons based on these flags

```cs
// Example controller
[CSDPermissions(["READ-DORA", "WRITE-DORA"])]
public class Dora : EntityController<PMS_DORA> { ... }

// Automatic permission check on every method
var res = await base.GetPage(req, query => query);
// res.canRead = user has "READ-DORA" permission
// res.canWrite = user has "WRITE-DORA" permission
```

---

## Injectable Hooks

Clients can inject custom logic at CRUD lifecycle hooks:

```cs
public static class Injectables
{
    public static void RunGetItem(object item, ControllerBase controller) { ... }
    public static void RunGetPage(List<object> items, ControllerBase controller) { ... }
    public static void RunCreate(object item, ControllerBase controller) { ... }
    public static void RunUpdate(object item, ControllerBase controller) { ... }
    public static void RunDelete(object item, ControllerBase controller) { ... }
    public static void RunLogicalDelete(object item, ControllerBase controller) { ... }
}
```

Services can plug in cross-cutting concerns (logging, audit, validation) at these points.
