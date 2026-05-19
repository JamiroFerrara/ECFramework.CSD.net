# Joins & Relationships

EF Core Include-based joining. No explicit LINQ Join syntax needed.

## Overview

Framework automatically eager-loads related entities via `ApplyIncludes()` on every CRUD call. For complex relationships, override with manual includes or filtered joins.

**No N+1 queries** — all related data fetched in single round-trip.

---

## Automatic Includes (Default Behavior)

Every GetItem, GetPage call automatically includes all navigation properties via reflection.

### How It Works

```cs
public async Task<Response<E>> GetPage<R>(Request<R> req, ...)
{
    IQueryable<E> query = ctx.Set<E>();
    // ...
    query = ApplyIncludes(query, ctx);  // ← Auto-includes all relationships
    query = ApplyOrderBy(query, req.OrderBy);
    
    res.items = await query.AsNoTracking().ToListAsync();
    return res;
}
```

### ApplyIncludes Implementation

```cs
public static IQueryable<E> ApplyIncludes(IQueryable<E> query, DbContext context)
{
    var entityType = context.Model.GetEntityTypes()
        .FirstOrDefault(t => t.ClrType == typeof(E));

    if (entityType != null)
    {
        var navigations = entityType.GetNavigations();

        foreach (var navigation in navigations)
            query = query.Include(navigation.Name);
    }

    return query;
}
```

**Result**: For `GetPage(MacroDati)`, automatically includes:
- `MacroAreaDORA`
- `AreaDGOV`
- `cla` (collection)

Without explicit code.

---

## Manual Includes (Custom Endpoints)

Override default behavior for specific endpoints.

### One-to-Many Include

```cs
public partial class Dora : EntityController<PMS_DORA>
{
    [HttpGet("GetWithMacrodati")]
    public Task<Response<PMS_DORA>> GetWithMacrodati(
        [FromBody] Request<PMS_DORA> req
    ) => base.GetPage(req, query =>
    {
        return query.Include(d => d.Macrodati);
    });
}
```

### Multi-Level Include

```cs
public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpGet("GetWithDetails")]
    public Task<Response<PMS_MACRODATI>> GetWithDetails(
        [FromBody] Request<PMS_MACRODATI> req
    ) => base.GetPage(req, query =>
    {
        return query
            .Include(m => m.MacroAreaDORA)
            .Include(m => m.AreaDGOV)
            .Include(m => m.cla)
                .ThenInclude(rel => rel.Cla);
    });
}
```

### Selective Include (Performance)

```cs
public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpGet("GetLite")]
    public Task<Response<PMS_MACRODATI>> GetLite(
        [FromBody] Request<PMS_MACRODATI> req
    ) => base.GetPage(req, query =>
    {
        // Include only critical relationships
        return query.Include(m => m.MacroAreaDORA);
        // Skip cla collection (expensive)
    });
}
```

---

## Filtered Joins (Where().Any())

Filter entities based on related data existence.

### Example: CLAs with Associated Macrodati

```cs
public partial class Cla : EntityController<CLA_CLA_PMS>
{
    [HttpGet("GetAssociated")]
    public Task<Response<CLA_CLA_PMS>> GetAssociated(
        [FromBody] Request<CLA_CLA_PMS> req
    ) => base.GetPage(req, query =>
    {
        // Get CLAs that have at least one Macrodati
        query = query.Where(x => x.Macrodati.Any());
        return query;
    });
}

[HttpGet("GetUnassociated")]
public Task<Response<CLA_CLA_PMS>> GetUnassociated(
    [FromBody] Request<CLA_CLA_PMS> req
) => base.GetPage(req, query =>
{
    // Get CLAs with NO Macrodati
    query = query.Where(x => !x.Macrodati.Any());
    return query;
});
```

**SQL Generated** (simplified):
```sql
-- GetAssociated
SELECT * FROM CLA 
WHERE EXISTS (SELECT 1 FROM CLA_TO_MACRODATI WHERE CLA_TO_MACRODATI.CodiceCLA = CLA.CodiceCLA)

-- GetUnassociated
SELECT * FROM CLA 
WHERE NOT EXISTS (SELECT 1 FROM CLA_TO_MACRODATI WHERE CLA_TO_MACRODATI.CodiceCLA = CLA.CodiceCLA)
```

### Complex Filter: Match Related Property

```cs
public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpPost("GetRelatedPage")]
    public Task<Response<PMS_MACRODATI>> GetPage(
        [FromBody] Request<CLA_CLA_PMS> req
    ) => base.GetPage(req, query =>
    {
        if (req.Item != null)
        {
            var hasAssociati = req.Like.TryGetValueAs<bool>(
                "Associati",
                out var associati
            );

            if (hasAssociati)
            {
                if (associati)
                    // Get Macrodati WITH this CLA
                    query = query.Where(x =>
                        x.cla.Where(c =>
                            c.CodiceCLA == req.Item.CodiceCLA &&
                            c.COD_ABI == req.Item.COD_ABI &&
                            c.Versione == req.Item.Versione &&
                            c.CodiceNodo == req.Item.CodiceNodo
                        ).Any()
                    );
                else
                    // Get Macrodati WITHOUT this CLA
                    query = query.Where(x =>
                        !x.cla.Where(c =>
                            c.CodiceCLA == req.Item.CodiceCLA &&
                            c.COD_ABI == req.Item.COD_ABI &&
                            c.Versione == req.Item.Versione &&
                            c.CodiceNodo == req.Item.CodiceNodo
                        ).Any()
                    );
            }
        }

        return query;
    });
}
```

**Frontend Usage**:
```typescript
// Get Macrodati associated with CLA
const response = await client.macroDati.getPage({
  Item: { CodiceCLA: "ABC", COD_ABI: "05000" },
  Like: { Associati: true }
});

// Get Macrodati NOT associated with CLA
const response = await client.macroDati.getPage({
  Item: { CodiceCLA: "ABC", ... },
  Like: { Associati: false }
});
```

---

## Relationship Types

### One-to-Many

```cs
public class PMS_DORA
{
    [Key]
    public string Id { get; set; }

    public virtual ICollection<PMS_MACRODATI> Macrodati { get; set; }
}

public class PMS_MACRODATI
{
    public string MacroAreaDORAId { get; set; }
    public virtual PMS_DORA MacroAreaDORA { get; set; }
}

// Include Pattern
query.Include(d => d.Macrodati)
query.Include(m => m.MacroAreaDORA)
```

### Many-to-Many via Junction Table

```cs
public class PMS_MACRODATI
{
    public virtual ICollection<CLA_TO_MACRODATI> cla { get; set; }
}

public class CLA_CLA_PMS
{
    public virtual ICollection<CLA_TO_MACRODATI> Macrodati { get; set; }
}

public class CLA_TO_MACRODATI
{
    public virtual PMS_MACRODATI Macrodati { get; set; }
    public virtual CLA_CLA_PMS Cla { get; set; }
}

// Include Pattern
query.Include(m => m.cla)
    .ThenInclude(rel => rel.Cla)
```

---

## Performance Considerations

### N+1 Query Problem

**Without includes** (BAD):
```cs
var macrodati = await ctx.Macrodati.ToListAsync();  // 1 query
foreach (var m in macrodati)
{
    var dora = m.MacroAreaDORA;  // N additional queries!
}
```

**With includes** (GOOD):
```cs
var macrodati = await ctx.Macrodati
    .Include(m => m.MacroAreaDORA)  // Auto-done by ApplyIncludes
    .ToListAsync();  // 1 query

foreach (var m in macrodati)
{
    var dora = m.MacroAreaDORA;  // Already loaded
}
```

---

## Best Practices

1. **Use ApplyIncludes for simple relationships** — auto-includes all
2. **Override includes for complex cases** — selective include for performance
3. **Use Where().Any() for existence checks** — generates EXISTS subquery
4. **ThenInclude for deep nesting** — traverse relationships
5. **Name endpoints by include strategy** — GetPage, GetLite, GetWithDetails
6. **Document include behavior** — specify what's included

---

## Summary Table

| Operation | Pattern | Use Case |
|-----------|---------|----------|
| **Auto-include all** | `ApplyIncludes()` | Default CRUD |
| **Manual include** | `.Include(x => x.Related)` | Custom endpoint |
| **Multi-level** | `.Include().ThenInclude()` | Junction tables |
| **Existence check** | `.Where(x => x.Items.Any())` | Filter by relationship |
| **Filtered join** | `.Where(x => x.Items.Any(i => ...))` | Match related property |
| **Exclusion** | `.Where(x => !x.Items.Any())` | Orphaned items |
