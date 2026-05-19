# TypeScript Client

Auto-generated type-safe API client via `swagger-typescript-api`.

## Generation

### Setup (First Time)

```bash
cd allitude-pms-fe
npx swagger-typescript-api@latest
```

Interactive prompts:
- **Swagger URL**: `http://localhost:5000/swagger.json` (backend service)
- **Output path**: `src/client`
- **Client name**: `Api`

### Auto Generation (Postinstall)

Edit `package.json`:

```json
{
  "scripts": {
    "prepare": "husky install && bash ./post-install.sh"
  }
}
```

In `post-install.sh`:

```bash
#!/bin/bash
npx swagger-typescript-api \
  -p ./swagger.json \
  -o ./src/client \
  -n client \
  --no-client
```

Runs on every `npm install`.

---

## Generated Structure

Generated file: `src/client/client.ts`

### Entity Types

Each entity gets Request + Response pair:

```typescript
// Entity model (from C# entity)
export interface PMS_DORA {
  Id?: string;
  Name?: string;
  Description?: string;
  CreatedAt?: string;  // @format date-time
  ModDate?: string | null;
  ModUser?: string | null;
  Abi?: string | null;
}

// Request wrapper
export interface PMSDORARequest {
  ctx?: CSDContext;
  pars?: CSDParam[] | null;
  flatten?: PMS_DORA;
  Equals?: PMS_DORA;
  Like?: Record<string, any>;              // Loose filter
  Expressions?: Record<string, any>;        // Strict filter
  Page?: number;
  PageSize?: number | null;
  OrderBy?: string | null;
  Schema?: StringStringValueTuple[] | null;
  Item?: PMS_DORA;
  Items?: PMS_DORA[] | null;
}

// Response wrapper
export interface PMSDORAResponse {
  ErrorGuid?: string | null;
  Rc?: number;  // Return code
  RcDescription?: string | null;
  RcInfo?: string | null;
  RcTimers?: CSDTimer[] | null;
  Formatted?: string | null;
  items?: PMS_DORA[] | null;
  item?: PMS_DORA;
  totalPages?: number;
  totalItems?: number;
  file?: string | null;  // For Excel export
  fileName?: string | null;
  canRead?: boolean;     // Permission flag
  canWrite?: boolean;    // Permission flag
}
```

### Api Class

Central class with method groups per entity:

```typescript
export class Api<SecurityDataType = unknown> extends HttpClient<SecurityDataType> {
  dora = {
    getPage: (query: {...}, params?: RequestParams) =>
      this.request<PMSDORAResponse, any>({...}),
    getItem: (query: {...}, params?: RequestParams) =>
      this.request<PMSDORAResponse, any>({...}),
    create: (data: PMS_DORA[], params?: RequestParams) =>
      this.request<PMSDORAResponse, any>({...}),
    update: (data: PMSDORARequest, params?: RequestParams) =>
      this.request<PMSDORAResponse, any>({...}),
    delete: (query: {...}, data?: PMS_DORA[], params?: RequestParams) =>
      this.request<PMSDORAResponse, any>({...}),
  };

  // ... other entity groups
}
```

---

## Usage in React

### Basic Setup

```typescript
// src/services/client/request.ts
import { Api } from 'src/client/client';

const api = new Api({
    baseURL: process.env.REACT_APP_API_URL || 'http://localhost:5000',
});

export default api;
```

### With useQuery Hook

```typescript
import client from 'services/client/request';
import { useQuery } from 'utils/hooks/useQuery';

function MyPage() {
  // Fully typed!
  const api = useQuery(client.dora.getPage, {
    PageSize: 10,
    Like: { Status: 'Active' },
    OrderBy: 'CreatedAt desc'
  });

  // Reactive query
  const handleFilter = (name: string) => {
    api.query({ Like: { Name: name } }, 1); // Reset to page 1
  };

  // Refetch
  const handleRefresh = async () => {
    await api.refetch();
  };

  return (
    <div>
      <input 
        placeholder="Filter by name"
        onChange={(e) => handleFilter(e.target.value)}
      />
      
      {api.isLoading && <Skeleton />}
      
      <table>
        <tbody>
          {api.state?.items?.map(item => (
            <tr key={item.Id}>
              <td>{item.Name}</td>
              <td>{item.Description}</td>
              {api.state.canWrite && (
                <td>
                  <button onClick={() => handleEdit(item)}>Edit</button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>

      <Pagination
        page={api.page}
        totalPages={api.state?.totalPages || 0}
        onPageChange={(p) => api.query({}, p)}
      />
    </div>
  );
}
```

### Create

```typescript
async function handleCreate(formData: PMS_DORA) {
  try {
    const response = await client.dora.create([formData]);
    
    if (response.canWrite) {
      showSuccess('Created successfully');
      api.refetch();  // Refresh list
    } else {
      showError('Permission denied');
    }
  } catch (error) {
    showError(error.message);
  }
}
```

### Update

```typescript
async function handleUpdate(id: string, updates: Partial<PMS_DORA>) {
  try {
    const response = await client.dora.update({
      Item: { ...existingItem, ...updates }
    });

    if (response.canWrite) {
      showSuccess('Updated successfully');
      api.refetch();
    } else {
      showError('Permission denied');
    }
  } catch (error) {
    showError(error.message);
  }
}
```

### Delete

```typescript
async function handleDelete(id: string) {
  try {
    const response = await client.dora.delete({
      Items: [{ Id: id }]
    });

    if (response.canWrite) {
      showSuccess('Deleted successfully');
      api.refetch();
    } else {
      showError('Permission denied');
    }
  } catch (error) {
    showError(error.message);
  }
}
```

### Excel Export

```typescript
async function handleExport() {
  try {
    const response = await client.dora.getExcel({
      Like: api.params.Like,
      OrderBy: api.params.OrderBy
    });

    // Download file
    const blob = new Blob([response.file], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = response.fileName;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
  } catch (error) {
    showError('Export failed: ' + error.message);
  }
}
```

---

## TypeScript Benefits

1. **Full type checking** on API calls
   - IDE autocomplete for all client methods
   - Type errors caught at compile time

2. **Type-safe request objects**
   - Can't pass wrong filter types
   - Schema validation at call site

3. **Type-safe responses**
   - All response properties typed
   - No `any` casts needed

4. **Circular reference support**
   - Complex nested relationships properly typed
   - No casting required in components

Example:

```typescript
// Type error caught immediately!
api.query({ Like: { InvalidField: 'test' } });  // ✗ TS error

// Proper usage
api.query({ Like: { Nome: 'test' } });  // ✓ OK

// Response is fully typed
api.state?.items?.forEach(item => {
  console.log(item.Name);  // ✓ TS knows this property exists
  console.log(item.InvalidProp);  // ✗ TS error
});
```

---

## Error Handling

Axios errors thrown if HTTP fails (non-2xx):

```typescript
try {
  const response = await client.dora.getPage({ Like: {...} });
} catch (error) {
  if (error.response?.status === 403) {
    console.log('Permission denied');
  } else if (error.response?.status === 400) {
    console.log('Bad request:', error.response.data);
  } else {
    console.log('Network error:', error.message);
  }
}
```

---

## Interceptor Chain

Every request is intercepted in `src/services/client/interceptorFunctions.ts`:

```typescript
// Automatically injected on every call:
api.instance.interceptors.request.use((config) => {
  // 1. Add authorization header
  config.headers.Authorization = `Bearer ${getToken()}`;
  
  // 2. Inject CSDContext
  if (!config.data) config.data = {};
  config.data.ctx = {
    user: {
      CodiceToken: getToken(),
      CodiceAbiDefault: getAbi(),
      // ...
    }
  };
  
  // 3. Add custom headers (if needed)
  config.headers['X-Allitude-Version'] = '1.0';
  
  return config;
});
```

So backend always receives authenticated context with every request.
