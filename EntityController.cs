using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

[Route("api/[controller]")]
[RequestHydrationFilter]
public partial class EntityController<E> : Controller where E : class, new()
{
    public DbContext ctx;
    public EntityController(DbContext ctx, IConfiguration configuration) { this.ctx = ctx; }

    [NonAction]
    public async Task<Response<E>> GetItem([FromBody] Request<E> req, Func<DbSet<E>, DbSet<E>> action)
    {
        return await Try<Response<E>>(async () =>
        {
            var ctx = this.ctx;
            var res = new Response<E>();

            // Get the DbSet instead of IQueryable
            IQueryable<E> query = ctx.Set<E>();

            if (req.Expressions != null && req.Expressions.Count > 0)
                query = ApplyWhere(query, req.Expressions, null, "");

            query = ApplyIncludes(query, ctx);
            query = ApplyOrderBy(query, req.OrderBy);

            res.item = await query
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (res.item == null)
                res.item = new E();

            Injectables.RunGetItem(res.item, this);

            // res.canRead = CanRead(actions);
            // res.canWrite = CanWrite(actions);

            return res;
        });
    }

    [NonAction]
    public async Task<Response<E>> GetPage<R>(Request<R> req, Func<IQueryable<E>, IQueryable<E>> action) where R : class, new()
    {
        return await Try<Response<E>>(async () =>
        {
            var ctx = this.ctx;
            // Get the queryable entity set
            IQueryable<E> query = ctx.Set<E>();
            req.PageSize = req.PageSize ?? 12;

            // If there are filters, dynamically apply them
            if (req.Expressions != null && req.Expressions.Count > 0)
                query = ApplyWhere(query, req.Expressions, null, "");

            query = ApplyIncludes(query, ctx);
            query = ApplyOrderBy(query, req.OrderBy);

            // Apply pagination
            var res = new Response<E>();

            query = action(query);

            res.items = (await query
                .AsNoTracking()
                .Skip((int)(req.Page * req.PageSize))
                .Take((int)req.PageSize)
                .ToListAsync()) as List<E>;

            Injectables.RunGetPage(res.items, this);

            var count = query.Count();

            var size = ((decimal)count / req.PageSize) ?? new decimal(1);
            res.totalPages = (int)Math.Ceiling(size);
            res.totalItems = count;

            // res.canRead = CanRead(actions);
            // res.canWrite = CanWrite(actions);

            return res;
        });
    }

    [NonAction]
    public async Task<Response<E>> GetExcel<R>(Request<R> req, Func<IQueryable<E>, IQueryable<E>> action) where R : class, new()
    {
        req.Page = 0;
        req.PageSize = 999999999; //Max int
        var res = await this.GetPage<R>(req, action);
        if (!res.canRead)
            throw new Exception("Utente non ha permessi di lettura.");

        string fileName = typeof(E).Name + ".xlsx";
        byte[] excel = res.items.ToExcel(fileName, req.Schema);

        res.file = excel;
        res.fileName = fileName;
        return res;
    }

    [NonAction]
    public async Task<Response<E>> Create(List<E> items, Request<E> req, Func<DbSet<E>, DbSet<E>> action)
    {
        return await Try<Response<E>>(async () =>
        {
            var ctx = this.ctx;
            var res = new Response<E>();

            this.CheckReflectiveId(items);
            DbSet<E> query = ctx.Set<E>();
            query = action(query);

            if (items != null)
                foreach (var item in items)
                {
                    query.Add(item);
                    Injectables.RunCreate(item, this);
                }

            await ctx.SaveChangesAsync();
            res.items = items;

            return res;
        });
    }

    [NonAction] //FIX: Multi update is broken and need to re-think this
    public async Task<Response<E>> Update([FromBody] Request<E> req, Func<IQueryable<E>, IQueryable<E>> action)
    {
        return await Try<Response<E>>(async () =>
        {
            var ctx = this.ctx;
            var res = new Response<E>(); //FIX: Throw error
            IQueryable<E> query = ctx.Set<E>();
            query = action(query);
            DbSet<E> dbSet = (DbSet<E>)query;

            // If there are filters, dynamically apply them
            if (req.Expressions != null && req.Expressions.Count() > 0 && req.Items == null)
            {
                query = ApplyWhere(query, req.Expressions, null, "", true);
                res.item = query.FirstOrDefault();
            }

            if (res.item != null)
            {
                Injectables.RunUpdate(req.Item, this);
                ctx.Entry(res.item).CurrentValues.SetValues(req.Item);
                await ctx.SaveChangesAsync();
            }

            //TODO: Multiupdates

            // res.canRead = CanRead(actions);
            // res.canWrite = CanWrite(actions);

            res.item = req.Item;
            return res;
        });
    }


    [NonAction]
    public virtual async Task<Response<E>> Delete([FromBody] Request<E> req, Func<IQueryable<E>, IQueryable<E>> action)
    {
        return await Try<Response<E>>(async () =>
        {
            var ctx = this.ctx;
            var res = new Response<E>(); //FIX: Throw error
            IQueryable<E> query = ctx.Set<E>();
            query = action(query);
            DbSet<E> dbSet = (DbSet<E>)query;

            // If there are filters, dynamically apply them
            if (req.Expressions != null && req.Expressions.Count() > 0 && req.Items == null)
            {
                query = ApplyWhere(query, req.Expressions, null, "", true);
                var items = await query.ToListAsync();

                res.items = items;
                req.Items = items;
            }

            //This is a test edit

            //Multi Delete
            if (req.Items != null)
                foreach (var item in req.Items)
                {
                    var found_item = dbSet.Local.FirstOrDefault(e => e == item);
                    if (found_item != null)
                    {
                        Injectables.RunDelete(found_item, this);
                        dbSet.Remove(found_item);
                    }
                }

            await ctx.SaveChangesAsync();

            // res.canRead = CanRead(actions);
            // res.canWrite = CanWrite(actions);

            return res;
        });
    }

    [NonAction]
    public virtual async Task<Response<E>> LogicalDelete([FromBody] Request<E> req, Func<IQueryable<E>, IQueryable<E>> action)
    {
        return await Try<Response<E>>(async () =>
        {
            var ctx = this.ctx;
            var res = new Response<E>();
            IQueryable<E> query = ctx.Set<E>();
            DbSet<E> dbSet = (DbSet<E>)query;
            query = action(query);

            // If there are filters, dynamically apply them
            if (req.Expressions != null && req.Expressions.Count > 0)
            {
                query = ApplyWhere(query, req.Expressions, null, "");
                res.items = await query.ToListAsync();
                req.Items = await query.ToListAsync();
            }

            Injectables.RunLogicalDelete(res.item, this);

            //TODO: Multi logical delete

            await ctx.SaveChangesAsync();

            // res.canRead = CanRead(actions);
            // res.canWrite = CanWrite(actions);

            return res;
        });
    }
}
