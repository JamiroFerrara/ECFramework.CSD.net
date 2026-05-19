# Backend Extensions

Custom endpoints & filtering in partial controller classes.

## Partial Controller Pattern

Split controller logic across multiple files using C# partial classes.

### Structure

```
Api/
├── Index.cs              # Controller registration
├── Dora/
│   ├── CustomEndpoint.cs # Custom business logic
├── MacroDati/
│   ├── Delete.cs        # Override Delete method
│   ├── GetRelatedPage.cs # Override GetPage with custom filtering
```

### Defining Partials

In `Api/Index.cs`, declare controller with `partial`:

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

Then create `Api/Dora/CustomMethod.cs`:

```cs
public partial class Dora : EntityController<PMS_DORA>
{
    [HttpGet("GetActive")]
    [CSDPermissions(["PMS-R", "PMS-W"])]
    public async Task<Response<PMS_DORA>> GetActive() =>
        await base.GetPage(
            new Request<PMS_DORA>(),
            query => query.Where(x => x.Active)
        );
}
```

---

## Override Base Methods

Replace standard CRUD behavior:

### Override GetPage with Custom Action

```cs
public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpPost("GetRelatedPage")]
    public Task<Response<PMS_MACRODATI>> GetPage(
        [FromBody] Request<CLA_CLA_PMS> req
    ) =>
        base.GetPage(req, query =>
        {
            var hasAssociati = req.Like.TryGetValueAs<bool>(
                "Associati",
                out var associati
            );

            if (req.Item != null && hasAssociati)
            {
                if (associati)
                    query = query.Where(x =>
                        x.cla.Where(c =>
                            c.CodiceCLA == req.Item.CodiceCLA &&
                            c.COD_ABI == req.Item.COD_ABI
                        ).Any()
                    );
                else
                    query = query.Where(x =>
                        !x.cla.Where(c =>
                            c.CodiceCLA == req.Item.CodiceCLA &&
                            c.COD_ABI == req.Item.COD_ABI
                        ).Any()
                    );
            }

            return query;
        });
}
```

### Custom Response Type

Return specialized response if needed:

```cs
[HttpPost("CreatePms")]
public async Task<CreatePmsResponseResponse> CreatePms(
    [FromBody] CreatePmsRequest req
)
{
    var pmsItems = new List<PMS_PMS>();
    var macrodatiItems = new List<PMS_PMS_MACRODATI>();

    // ... complex logic ...

    var response = new CreatePmsResponse
    {
        pms = pmsItems,
        pms_macrodati = macrodatiItems,
    };

    return new CreatePmsResponseResponse
    {
        item = response,
        canRead = CanRead(null),
        canWrite = CanWrite(null),
    };
}
```

---

## Custom Filtering Actions

Inject custom IQueryable transformations:

### Based on User Context

```cs
public partial class User : EntityController<PMS_USER>
{
    [HttpGet("GetPage")]
    public async Task<Response<PMS_USER>> GetPage(
        [FromBody] Request<PMS_USER> req
    ) =>
        await base.GetPage(req, query =>
        {
            var userAbi = this.GetUserAbi();
            return query.Where(x => x.Abi == userAbi);
        });
}
```

### Business Logic Rules

```cs
public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpGet("GetPage")]
    public async Task<Response<PMS_MACRODATI>> GetPage(
        [FromBody] Request<PMS_MACRODATI> req
    ) =>
        await base.GetPage(req, query =>
        {
            var userId = this.GetUserId();
            return query.Where(x =>
                x.Stato == "Pubblicato" ||
                (x.Stato == "Bozza" && x.ModUser == userId)
            );
        });
}
```

### Complex Relationships

```cs
public partial class Pms : EntityController<PMS_PMS>
{
    [HttpGet("GetPage")]
    public async Task<Response<PMS_PMS>> GetPage(
        [FromBody] Request<PMS_PMS> req
    ) =>
        await base.GetPage(req, query =>
        {
            return query
                .Include(p => p.Servizi)
                .Where(p =>
                    p.Servizi.Any(s =>
                        s.Stato == "Active"
                    )
                );
        });
}
```

---

## Custom Endpoints (Non-CRUD)

Add specialized business endpoints:

### Bulk Operations

```cs
[HttpPost("PublishBatch")]
[CSDPermissions(["PMS-R", "PMS-W"])]
public async Task<Response<CLA_CLA_PMS>> PublishBatch(
    [FromBody] List<string> claIds
)
{
    return await Try<Response<CLA_CLA_PMS>>(async actions =>
    {
        var items = await ctx.Set<CLA_CLA_PMS>()
            .Where(c => claIds.Contains(c.CodiceCLA))
            .ToListAsync();

        foreach (var item in items)
        {
            item.Stato = 1;
            item.ModDate = DateTime.UtcNow;
            Injectables.RunUpdate(item, this);
        }

        await ctx.SaveChangesAsync();

        return new Response<CLA_CLA_PMS>
        {
            items = items,
            canRead = CanRead(actions),
            canWrite = CanWrite(actions),
        };
    }, Permissions.Write);
}
```

### Aggregate Data

```cs
[HttpGet("GetStats")]
public async Task<Response<StatsResponse>> GetStats()
{
    return await Try<Response<StatsResponse>>(async actions =>
    {
        var query = ctx.Set<PMS_MACRODATI>()
            .Where(m => m.IsDeleted == false);

        var stats = new StatsResponse
        {
            TotalMacrodati = await query.CountAsync(),
            PublishedCount = await query
                .Where(m => m.Stato == "Pubblicato")
                .CountAsync(),
            DraftCount = await query
                .Where(m => m.Stato == "Bozza")
                .CountAsync(),
        };

        return new Response<StatsResponse>
        {
            item = stats,
            canRead = CanRead(actions),
        };
    }, Permissions.Read);
}
```

---

## Safe Data Modification

Always use `Try<T>` wrapper for permissions + error handling:

```cs
[HttpPost("ResetPassword")]
[CSDPermissions(["ADMIN-R", "ADMIN-W"])]
public async Task<Response<PMS_USER>> ResetPassword(
    [FromBody] ResetPasswordRequest req
)
{
    return await Try<Response<PMS_USER>>(async actions =>
    {
        var user = await ctx.Set<PMS_USER>()
            .Where(u => u.Id == req.UserId)
            .FirstOrDefaultAsync();

        if (user == null)
            throw new Exception("User not found");

        user.PasswordHash = HashPassword(req.NewPassword);
        user.PasswordResetRequired = false;
        user.ModDate = DateTime.UtcNow;
        user.ModUser = GetUserId();

        Injectables.RunUpdate(user, this);
        await ctx.SaveChangesAsync();

        return new Response<PMS_USER>
        {
            item = user,
            canRead = CanRead(actions),
            canWrite = CanWrite(actions),
        };
    }, Permissions.Write);
}
```

---

## Controller Helpers

```cs
protected string GetUserId() => 
    Request.HttpContext.Items["UserId"]?.ToString();

protected string GetUserAbi() =>
    Request.HttpContext.Items["UserAbi"]?.ToString();

protected bool CanRead(dynamic actions) =>
    // Checks [CSDPermissions] read permission

protected bool CanWrite(dynamic actions) =>
    // Checks [CSDPermissions] write permission
```
