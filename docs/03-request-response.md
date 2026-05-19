# Request/Response Models

Standard DTOs for all API communication.

## Request<T>

Generic input DTO for all CRUD operations.

### Definition

```cs
public class Request<T>
{
    public CSDContext ctx { get; set; }
    public CSDParam[] pars { get; set; }
    public T flatten { get; set; }
    public T Equals { get; set; }
    public Dictionary<string, object> Like { get; set; }
    public Dictionary<string, object> Expressions { get; set; }
    public int Page { get; set; }
    public int? PageSize { get; set; }
    public string OrderBy { get; set; }
    public StringStringValueTuple[] Schema { get; set; }
    public T Item { get; set; }
    public List<T> Items { get; set; }
}
```

### Fields Explained

| Field | Type | Usage | Example |
|-------|------|-------|---------|
| `ctx` | `CSDContext` | Request context (user, app, device) | Auto-injected by framework |
| `pars` | `CSDParam[]` | Custom parameters | `[{pn: "key", pv: "value"}]` |
| `Like` | `Dict<string,obj>` | Loose filtering (contains) | `{ "Nome": "test" }` |
| `Expressions` | `Dict<string,obj>` | Strict filtering (equals) | `{ "Status": "Active" }` |
| `Page` | `int` | 0-indexed page number | `0` = first page |
| `PageSize` | `int?` | Items per page (default: 12) | `20` |
| `OrderBy` | `string` | Sort field + direction | `"CreatedAt"` or `"CreatedAt desc"` |
| `Schema` | `Tuple[]` | Excel column mapping | `[{key: "Id", value: "ID"}]` |
| `Item` | `T` | Single entity for update/delete | One entity instance |
| `Items` | `List<T>` | Multiple entities for batch ops | List of entity instances |

### Filtering Patterns

#### Like (Loose/Contains)

```typescript
// Frontend
api.query({
  Like: {
    Nome: "test",           // Contains "test"
    Descrizione: "report",  // Contains "report"
    Stato: "Active"         // String match (case-insensitive)
  }
});

// Generated as query string for GET
?Like={"Nome":"test","Descrizione":"report","Stato":"Active"}
```

Backend applies `WHERE Nome LIKE '%test%' AND Descrizione LIKE '%report%'`

#### Expressions (Strict/Exact)

```typescript
// Frontend
api.query({
  Expressions: {
    Id: "12345",     // Exact match
    Status: 1,       // Numeric comparison
    IsActive: true   // Boolean match
  }
});

// Generated as query string
?Expressions={"Id":"12345","Status":1,"IsActive":true}
```

Backend applies `WHERE Id = '12345' AND Status = 1 AND IsActive = true`

#### Combined Filters

```typescript
api.query({
  Like: { Nome: "test" },
  Expressions: { Stato: "Pubblicato" },
  PageSize: 10,
  OrderBy: "CreatedAt desc"
});

// SQL equivalent (simplified)
// WHERE Nome LIKE '%test%' 
//   AND Stato = 'Pubblicato'
// ORDER BY CreatedAt DESC
// LIMIT 10
```

### Pagination

```typescript
// Page 0, size 20
api.query({
  Page: 0,
  PageSize: 20
});

// Skip = Page * PageSize = 0 * 20 = 0
// Take = PageSize = 20
// Returns items 0-19

api.query({}, 2);  // Jump to page 2
// Skip = 2 * 20 = 40
// Take = 20
// Returns items 40-59
```

### Sorting

```typescript
// Ascending
OrderBy: "CreatedAt"
OrderBy: "Nome"

// Descending
OrderBy: "CreatedAt desc"
OrderBy: "Price desc"

// Multiple? (Not yet supported, chain single calls)
api.query({ OrderBy: "Stato" });
```

### Context Injection

Auto-populated by `[RequestHydrationFilter]`:

```typescript
// Frontend sends just:
{ Like: { Nome: "test" } }

// Framework injects:
{
  ctx: {
    user: {
      CodiceToken: "abc123",
      CodiceAbiDefault: "05000",
      CodiceHolding: "ALLITUDE",
      DescrizioneUtente: "John Doe",
      ProfiloUtente: "ADMIN"
    },
    application: {
      CodApplicazione: 1,
      Environment: "Production"
    },
    device: {
      Type: "desktop",
      Modello: "Chrome"
    }
  },
  Like: { Nome: "test" },  // Original request
  Page: 0,
  PageSize: 12
}
```

---

## Response<T>

Generic output DTO for all CRUD results.

### Definition

```cs
public class Response<T>
{
    public string ErrorGuid { get; set; }
    public int Rc { get; set; }
    public string RcDescription { get; set; }
    public string RcInfo { get; set; }
    public CSDTimer[] RcTimers { get; set; }
    public object RcPayload { get; set; }
    public string Formatted { get; set; }
    public List<T> items { get; set; }
    public T item { get; set; }
    public int totalPages { get; set; }
    public int totalItems { get; set; }
    public byte[] file { get; set; }
    public string fileName { get; set; }
    public bool canRead { get; set; }
    public bool canWrite { get; set; }
}
```

### Fields Explained

| Field | Type | Usage | Example |
|-------|------|-------|---------|
| `ErrorGuid` | `string` | Unique error ID for tracing | `"abc-123-def"` |
| `Rc` | `int` | Return code (0 = success) | `0` or error code |
| `RcDescription` | `string` | User-friendly error message | `"Item not found"` |
| `RcInfo` | `string` | Technical error details | Stack trace or SQL error |
| `RcTimers` | `Timer[]` | Performance metrics | `[{description: "DB", elapsed: 42}]` |
| `items` | `List<T>` | Multiple results (GetPage, GetExcel) | Entity list |
| `item` | `T` | Single result (GetItem, Create, Update) | One entity |
| `totalPages` | `int` | Pagination: total pages | `5` |
| `totalItems` | `int` | Pagination: total count | `127` |
| `file` | `byte[]` | Binary file (Excel) | Base64-encoded bytes |
| `fileName` | `string` | File name hint | `"Export.xlsx"` |
| `canRead` | `bool` | Permission flag: read allowed | `true/false` |
| `canWrite` | `bool` | Permission flag: write allowed | `true/false` |

### Success Response

```typescript
// GetPage success
{
  items: [
    { Id: "1", Nome: "Item 1", ... },
    { Id: "2", Nome: "Item 2", ... }
  ],
  totalPages: 3,
  totalItems: 27,
  canRead: true,
  canWrite: true,
  Rc: 0
}

// GetItem success
{
  item: { Id: "1", Nome: "Item 1", ... },
  canRead: true,
  canWrite: true,
  Rc: 0
}

// Create success
{
  items: [
    { Id: "new-id", Nome: "New Item", ... }
  ],
  canRead: true,
  canWrite: true,
  Rc: 0
}
```

### Error Response

```typescript
// Permission denied
{
  canRead: false,
  canWrite: false,
  Rc: 403,
  RcDescription: "Access denied",
  ErrorGuid: "err-abc-123"
}

// Validation error
{
  Rc: 400,
  RcDescription: "Invalid input",
  RcInfo: "Field 'Nome' is required",
  ErrorGuid: "err-def-456"
}

// Server error
{
  Rc: 500,
  RcDescription: "Internal server error",
  RcInfo: "System.NullReferenceException at line 123",
  ErrorGuid: "err-ghi-789"
}
```

### Pagination Example

```typescript
// Request
{
  Page: 1,
  PageSize: 10
}

// Response
{
  items: [ /* 10 items */ ],
  totalItems: 127,
  totalPages: 13,  // Math.ceil(127 / 10) = 13
  canRead: true,
  canWrite: false
}

// Frontend knows:
// - Showing items 11-20 (page 1, size 10)
// - 3 pages remaining (13 - 1 = 12 pages left)
// - Total database count is 127
```

### Excel Export Response

```typescript
// Request
{
  Like: { Stato: "Pubblicato" },
  Schema: [
    { key: "Id", value: "ID" },
    { key: "Nome", value: "Nome" },
    { key: "Descrizione", value: "Descrizione" }
  ]
}

// Response
{
  file: UInt8Array([...bytes...]),  // Binary data
  fileName: "Macrodati.xlsx",
  canRead: true,
  canWrite: false,
  Rc: 0
}

// Frontend downloads file
const blob = new Blob([response.file]);
const url = window.URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = url;
a.download = response.fileName;
a.click();
```

### Permission Flags in Response

Always check before showing UI:

```typescript
const response = await client.dora.getPage({...});

// Show/hide edit button
{response.canWrite && <EditButton />}

// Show/hide delete button
{response.canWrite && <DeleteButton />}

// Show/hide download button
{response.canRead && <DownloadButton />}

// Read-only mode
if (!response.canWrite) {
  formik.setFieldDisabled = true;  // Disable all fields
}
```

---

## CSDContext

Request context carrying user, app, device info.

### Definition

```cs
public class CSDContext
{
    public User user { get; set; }
    public Application application { get; set; }
    public Device device { get; set; }
}

public class User
{
    public string CodiceToken { get; set; }           // Auth token
    public string CodiceAbiDefault { get; set; }      // Default bank
    public string CodiceHolding { get; set; }         // Holding
    public string CodiceAbiUser { get; set; }         // User's bank
    public string CodiceUtente { get; set; }          // User ID
    public string DescrizioneHolding { get; set; }    // Holding name
    public string DescrizioneBancaDefault { get; set; } // Bank name
    public string DescrizioneUtente { get; set; }     // User name
    public string ProfiloUtente { get; set; }         // Role/profile
}

public class Application
{
    public int CodApplicazione { get; set; }
    public string DescrizioneApplicazione { get; set; }
    public AppEnvironment Environment { get; set; }
}

public enum AppEnvironment
{
    Value0 = 0,    // Development
    Value1 = 1,    // Test
    Value2 = 2,    // Staging
    Value3 = 3,    // Production
    // ...
}

public class Device
{
    public string Type { get; set; }    // "mobile", "desktop"
    public string Modello { get; set; } // "iPhone", "Chrome"
}
```

### Usage in Backend

```cs
// In custom endpoint
[HttpGet("MyData")]
public async Task<Response<MyEntity>> GetMyData(
    [FromBody] Request<MyEntity> req
)
{
    var userId = req.ctx?.user?.CodiceUtente;
    var userAbi = req.ctx?.user?.CodiceAbiDefault;
    var userRole = req.ctx?.user?.ProfiloUtente;

    var query = ctx.Set<MyEntity>()
        .Where(x => x.OwnerId == userId)
        .Where(x => x.Abi == userAbi);

    // Authorization check
    if (userRole != "ADMIN" && userRole != "MANAGER")
        return new Response<MyEntity>
        {
            Rc = 403,
            RcDescription = "Insufficient permissions"
        };

    return new Response<MyEntity>
    {
        items = await query.ToListAsync(),
        canRead = true,
        canWrite = userRole == "ADMIN"
    };
}
```

---

## Common Response Codes

| Code | Meaning | Action |
|------|---------|--------|
| `0` | Success | Continue, use data |
| `400` | Bad request | Check `RcInfo` for validation errors |
| `403` | Forbidden | Check permission flags, show UI message |
| `404` | Not found | Item deleted or invalid ID |
| `500` | Server error | Log `ErrorGuid`, show generic message |

---

## Testing Request/Response

```typescript
// Mock successful response
jest.spyOn(client.dora, 'getPage').mockResolvedValue({
  items: [{ Id: '1', Name: 'Test' }],
  totalPages: 1,
  totalItems: 1,
  canRead: true,
  canWrite: true,
  Rc: 0
});

// Mock permission denied
jest.spyOn(client.dora, 'getPage').mockResolvedValue({
  items: null,
  canRead: false,
  canWrite: false,
  Rc: 403,
  RcDescription: 'Access denied'
});

// Mock validation error
jest.spyOn(client.dora, 'create').mockRejectedValue(
  new Error('Field validation failed')
);
```
