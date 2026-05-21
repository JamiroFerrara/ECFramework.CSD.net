//Keep These usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECFramework;

[Route("api/[controller]")]
[RequestHydrationFilter]
public partial class EntityController<E> : CSDFrameworkPMSPatch.CSDController where E : class, new()
{
    public DbContext ctx;
    public EntityController(DbContext ctx, IConfiguration configuration) : base(configuration) { this.ctx = ctx; }

    [NonAction]
    public async Task<Response<E>> GetItem([FromBody] Request<E> req, Func<DbSet<E>, DbSet<E>> action)
    {
        return await Try<Response<E>>(async actions =>
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

            res.canRead = CanRead(actions);
            res.canWrite = CanWrite(actions);

            return res;
        }, Permissions.Read);
    }

    [NonAction]
    public async Task<Response<E>> GetPage<R>(Request<R> req, Func<IQueryable<E>, IQueryable<E>> action) where R : class, new()
    {
        return await Try<Response<E>>(async actions =>
        {
            // Get the queryable entity set
            var ctx = this.ctx;
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

            res.canRead = CanRead(actions);
            res.canWrite = CanWrite(actions);

            return res;
        }, Permissions.Read);
    }

    [NonAction]
    public async Task<Response<E>> GetExcel<R>(Request<R> req, Func<IQueryable<E>, IQueryable<E>> action) where R : class, new()
    {
        // If Items provided, generate Excel from those items
        if (req.Items != null && req.Items.Count > 0)
        {
            return await Try<Response<E>>(async actions =>
            {
                var res = new Response<E>();
                
                string fileName = typeof(E).Name + ".xlsx";
                byte[] excel = req.Items.ToExcel(fileName, req.Schema);
                
                res.file = excel;
                res.fileName = fileName;
                return res;
            }, Permissions.Read);
        }
        
        // Fallback to original query-based flow
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
        return await Try<Response<E>>(async actions =>
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
        }, Permissions.Write);
    }

    [NonAction]
    public async Task<Response<E>> Update([FromBody] Request<E> req, Func<IQueryable<E>, IQueryable<E>> action)
    {
        return await Try<Response<E>>(async actions =>
        {
            var ctx = this.ctx;
            var res = new Response<E>(); //FIX: Throw error
            IQueryable<E> query = ctx.Set<E>();
            query = action(query);
            DbSet<E> dbSet = (DbSet<E>)query;

            // If there are filters, dynamically apply them
            if (req.Expressions != null && req.Expressions.Count() > 0 && req.Items == null)
            {
                query = ApplyWhere(query, req.Expressions, null, "");
                res.item = query.FirstOrDefault();
            }

            if (res.item != null)
            {
                Injectables.RunUpdate(req.Item, this);
                ctx.Entry(res.item).CurrentValues.SetValues(req.Item);
                await ctx.SaveChangesAsync();
            }

            //TODO: Multiple updates?

            res.canRead = CanRead(actions);
            res.canWrite = CanWrite(actions);

            res.item = req.Item;
            return res;
        }, Permissions.Write);
    }


    [NonAction]
    public virtual async Task<Response<E>> Delete([FromBody] Request<E> req, Func<IQueryable<E>, IQueryable<E>> action)
    {
        return await Try<Response<E>>(async actions =>
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
                    var found_item = dbSet.FirstOrDefault(e => e == item);
                    if (found_item != null)
                    {
                        Injectables.RunDelete(found_item, this);
                        dbSet.Remove(found_item);
                    }
                }

            await ctx.SaveChangesAsync();

            res.canRead = CanRead(actions);
            res.canWrite = CanWrite(actions);

            return new Response<E>();
        }, Permissions.Write);
    }

    [NonAction]
    public virtual async Task<Response<E>> LogicalDelete([FromBody] Request<E> req, Func<IQueryable<E>, IQueryable<E>> action)
    {
        return await Try<Response<E>>(async actions =>
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
                var items = await query.ToListAsync();
                res.items = items;
                res.item = items.FirstOrDefault();
                req.Item = items.FirstOrDefault();
            }

            Injectables.RunLogicalDelete(res.item, this);

            //TODO: Multi logical delete

            await ctx.SaveChangesAsync();

            res.canRead = CanRead(actions);
            res.canWrite = CanWrite(actions);

            return res;
        }, Permissions.Write);
    }
}
