# Database Patterns

EF Core + Dapper integration, soft deletes, and data modeling.

## DbContext Setup

Single DbContext shared between EF and Dapper:

```cs
public class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<PMS_DORA> Dora { get; set; }
    public DbSet<PMS_MACRODATI> Macrodati { get; set; }
    public DbSet<CLA_CLA_PMS> Cla { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PMS_DORA>()
            .HasKey(x => x.Id);

        // Composite keys
        modelBuilder.Entity<CLA_TO_MACRODATI>()
            .HasKey(x => new { x.MacrodatiId, x.CodiceCLA, x.COD_ABI, x.CodiceNodo });

        // Relationships
        modelBuilder.Entity<PMS_MACRODATI>()
            .HasOne(x => x.MacroAreaDORA)
            .WithMany()
            .HasForeignKey(x => x.MacroAreaDORAId);

        // Indexes
        modelBuilder.Entity<PMS_MACRODATI>()
            .HasIndex(x => x.Stato)
            .HasName("IDX_Macrodati_Stato");

        // Global query filters (soft deletes)
        modelBuilder.Entity<PMS_MACRODATI>()
            .HasQueryFilter(x => x.IsDeleted == false);
    }
}
```

---

## Entity Modeling

### IKeyable

Mark entities with primary keys:

```cs
public interface IKeyable { }

public class PMS_DORA : IKeyable
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<PMS_MACRODATI> Macrodati { get; set; }
}
```

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
    [Key]
    public string Id { get; set; }

    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

### IModifiable

Track audit info:

```cs
public interface IModifiable
{
    DateTime? ModDate { get; set; }
    string ModUser { get; set; }
}

public class PMS_MACRODATI : ISoftDeletable, IModifiable
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ModDate { get; set; }
    public string ModUser { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

---

## Relationships

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
    [Key]
    public string Id { get; set; }

    public string MacroAreaDORAId { get; set; }
    public virtual PMS_DORA MacroAreaDORA { get; set; }
}

// Usage
query.Include(m => m.MacroAreaDORA)
    .ThenInclude(d => d.Macrodati)
```

### Many-to-Many via Junction Table

```cs
public class PMS_MACRODATI
{
    public virtual ICollection<CLA_TO_MACRODATI> cla { get; set; }
}

public class CLA_CLA_PMS
{
    [Key]
    public string CodiceCLA { get; set; }

    public virtual ICollection<CLA_TO_MACRODATI> Macrodati { get; set; }
}

public class CLA_TO_MACRODATI
{
    [Key]
    public string MacrodatiId { get; set; }

    [Key]
    public string CodiceCLA { get; set; }

    public string TipologiaDiRelazione { get; set; }

    public virtual PMS_MACRODATI Macrodati { get; set; }
    public virtual CLA_CLA_PMS Cla { get; set; }
}

// Usage
query.Include(m => m.cla)
    .ThenInclude(rel => rel.Cla)
```

### Composite Keys

```cs
[Table("CLA_TO_MACRODATI")]
public class CLA_TO_MACRODATI
{
    [Key]
    [Column(Order = 0)]
    public string MacrodatiId { get; set; }

    [Key]
    [Column(Order = 1)]
    public string CodiceCLA { get; set; }

    [Key]
    [Column(Order = 2)]
    public string COD_ABI { get; set; }

    [Key]
    [Column(Order = 3)]
    public int CodiceNodo { get; set; }
}

// Model configuration
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<CLA_TO_MACRODATI>()
        .HasKey(x => new { 
            x.MacrodatiId, 
            x.CodiceCLA, 
            x.COD_ABI, 
            x.CodiceNodo 
        });
}
```

---

## Soft Deletes (Logical Delete)

### Implementation

Entities implementing `ISoftDeletable` support soft delete:

```cs
public class PMS_MACRODATI : ISoftDeletable
{
    [Key]
    public string Id { get; set; }

    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

### Query Filter (Hide Deleted)

Automatically exclude soft-deleted items:

```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Global query filter: exclude deleted
    modelBuilder.Entity<PMS_MACRODATI>()
        .HasQueryFilter(x => x.IsDeleted == false);
}

// Now all queries automatically filter IsDeleted == false
var items = ctx.Macrodati.ToList();
// SQL: SELECT * FROM Macrodati WHERE IsDeleted = false
```

### LogicalDelete Method

Soft delete via framework:

```cs
[HttpDelete("SoftDelete")]
public async Task<Response<PMS_MACRODATI>> LogicalDelete(
    [FromBody] Request<PMS_MACRODATI> req
)
{
    return await base.LogicalDelete(req, query => query);
}

// Usage frontend
await client.macroDati.logicalDelete({
    Like: { Id: '123' }
});

// Backend sets DeletedAt and IsDeleted = true
```

### Permanently Delete

Hard delete (only if soft delete not sufficient):

```cs
[HttpDelete("PermanentDelete")]
[CSDPermissions(["ADMIN", "ADMIN"])]
public async Task<Response<PMS_MACRODATI>> PermanentDelete(
    [FromBody] Request<PMS_MACRODATI> req
)
{
    return await base.Delete(req, query => query);
}
```

---

## Dapper Integration

Use Dapper for raw SQL + EF for queries on same connection:

```cs
using Dapper;

public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpPost("BulkImport")]
    public async Task<Response<PMS_MACRODATI>> BulkImport(
        [FromBody] List<PMS_MACRODATI> items
    )
    {
        var connection = ctx.Database.GetDbConnection();

        const string sql = @"
            INSERT INTO PMS_MACRODATI (Id, Nome, Descrizione, Stato, CreatedAt, IsDeleted)
            VALUES (@Id, @Nome, @Descrizione, @Stato, @CreatedAt, @IsDeleted)
        ";

        await connection.ExecuteAsync(sql, items);

        return new Response<PMS_MACRODATI> { items = items };
    }
}
```

---

## Indexes for Performance

Define indexes in OnModelCreating:

```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Single column
    modelBuilder.Entity<PMS_MACRODATI>()
        .HasIndex(x => x.Stato)
        .HasName("IDX_Macrodati_Stato");

    // Composite index
    modelBuilder.Entity<PMS_MACRODATI>()
        .HasIndex(x => new { x.Stato, x.CreatedAt })
        .HasName("IDX_Macrodati_Stato_CreatedAt");

    // Unique constraint
    modelBuilder.Entity<PMS_DORA>()
        .HasIndex(x => x.Code)
        .IsUnique()
        .HasName("UNQ_Dora_Code");
}
```

---

## Migration Example

Add entity and migrate:

```bash
dotnet ef migrations add AddMacrodati
dotnet ef database update
dotnet ef database update [PreviousMigrationName]  # Revert
```

---

## Best Practices

1. **Always implement IKeyable** — GetItem depends on it
2. **Use ISoftDeletable for logical deletes** — Safer than hard delete
3. **Add indexes on frequently-filtered fields** — Stato, CreatedAt, UserId
4. **Track audit fields (IModifiable)** — CreatedAt, ModDate, ModUser
5. **Global query filters for soft deletes** — No manual checks needed
6. **Use Dapper only for bulk ops** — EF is safer, Dapper is faster
7. **Test soft delete behavior** — Verify hiding and restoration
