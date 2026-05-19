# Permissions

Access control via `[CSDPermissions]` attribute.

## Declaration

Mark controller with permission keys:

```cs
[CSDPermissions(["PMS-READ", "PMS-WRITE"])]
public class MacroDati : EntityController<PMS_MACRODATI>
{
    // All CRUD methods automatically protected
}
```

### Array Elements

- **Index 0**: Read permission key (for GetItem, GetPage, GetExcel)
- **Index 1**: Write permission key (for Create, Update, Delete, LogicalDelete)

```cs
[CSDPermissions(["READ", "WRITE"])]
//                 ↑        ↑
//                 |        └─ Used in Create, Update, Delete
//                 └────────── Used in GetItem, GetPage
```

## Framework Behavior

When `[CSDPermissions]` is present:

1. **Request arrives** with `ctx.user.ProfiloUtente` and permissions list
2. **EntityController checks** if user has required permission
3. **If denied**: Returns Response with:
   - `canRead: false` (if read permission missing)
   - `canWrite: false` (if write permission missing)
   - `Rc: 403` (optional, if you want to fail the request)
4. **If allowed**: Executes method, returns data with flags set to `true`

## Permission Check in Code

```cs
// Inside controller
protected bool CanRead(dynamic actions)
{
    // Returns true if user has read permission
    // (1st element of [CSDPermissions])
}

protected bool CanWrite(dynamic actions)
{
    // Returns true if user has write permission
    // (2nd element of [CSDPermissions])
}

// Usage
public async Task<Response<MyEntity>> Custom()
{
    return await Try<Response<MyEntity>>(async actions =>
    {
        if (!CanWrite(actions))
            return new Response<MyEntity>
            {
                Rc = 403,
                RcDescription = "Write permission required"
            };

        // Safe to modify
        await ctx.SaveChangesAsync();

        return new Response<MyEntity> { ... };
    }, Permissions.Write);
}
```

## Frontend Handling

Always check response flags before allowing operations:

```typescript
function MacroDatiPage() {
  const api = useQuery(client.macroDati.getPage, {...});

  return (
    <>
      {/* Show table only if can read */}
      {api.state?.canRead ? (
        <table>
          {api.state?.items?.map(item => (
            <tr key={item.Id}>
              <td>{item.Nome}</td>
              {/* Show edit button only if can write */}
              {api.state?.canWrite && (
                <td>
                  <button onClick={() => handleEdit(item)}>Edit</button>
                </td>
              )}
            </tr>
          ))}
        </table>
      ) : (
        <p>You don't have read access to this data</p>
      )}

      {/* Show create button only if can write */}
      {api.state?.canWrite && (
        <button onClick={() => navigate('create')}>Create New</button>
      )}
    </>
  );
}
```

## Permission Bypass (Development Only)

For local testing, set `de.bug` file:

```
IgnorePermissions=true
```

**WARNING**: Never use in production!

Backend will:
- Skip all permission checks
- Set `canRead = true`, `canWrite = true` on all responses
- Allow all operations

---

## Custom Permission Logic

Override permission checks in partial controller:

```cs
public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    // Override to add business logic
    protected new bool CanWrite(dynamic actions)
    {
        // Base check
        if (!base.CanWrite(actions))
            return false;

        // Additional rule: Only during business hours
        var hour = DateTime.UtcNow.Hour;
        if (hour < 8 || hour > 18)
            return false;  // Deny writes outside 8am-6pm

        return true;
    }
}
```

## Testing Permissions

Mock user context to test permissions:

```cs
[TestMethod]
public async Task GetPage_WithoutReadPermission_DeniesAccess()
{
    // Arrange
    var mockContext = new TestDbContext();
    var controller = new MacroDati(mockContext, config);
    
    // Mock user without permission
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            Items = new Dictionary<object, object>
            {
                { "UserPermissions", new[] { "OTHER" } }
                // Missing "PMS-READ"
            }
        }
    };

    // Act
    var response = await controller.GetPage(
        new Request<PMS_MACRODATI> { PageSize = 10 }
    );

    // Assert
    Assert.IsFalse(response.canRead);
    Assert.IsFalse(response.canWrite);
}

[TestMethod]
public async Task Create_WithWritePermission_Succeeds()
{
    // Arrange
    var mockContext = new TestDbContext();
    var controller = new MacroDati(mockContext, config);
    
    // Mock user with permission
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            Items = new Dictionary<object, object>
            {
                { "UserPermissions", new[] { "PMS-READ", "PMS-WRITE" } }
            }
        }
    };

    // Act
    var response = await controller.Create(
        new List<PMS_MACRODATI> { new PMS_MACRODATI { Nome = "Test" } },
        new Request<PMS_MACRODATI>()
    );

    // Assert
    Assert.IsTrue(response.canWrite);
    Assert.AreEqual(1, response.items.Count);
}
```

## Best Practices

1. **Always check permission flags in frontend**
   ```typescript
   if (!api.state?.canWrite) return <ReadOnlyView />;
   ```

2. **Never trust client-side permission checks alone**
   - Backend MUST always validate

3. **Use meaningful permission names**
   ```cs
   [CSDPermissions(["MACRODATI-VISUALIZZA", "MACRODATI-MODIFICA"])]
   ```

4. **Document permission requirements**
   ```cs
   /// <summary>
   /// Returns published macrodati.
   /// Requires: MACRODATI-VISUALIZZA permission
   /// </summary>
   [HttpGet("GetPage")]
   public Task<Response<PMS_MACRODATI>> GetPage(...)
   ```

5. **Test all permission combinations**
   - With permission: should succeed
   - Without permission: should deny with appropriate flags
