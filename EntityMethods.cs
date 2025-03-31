using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECFramework;

public partial class EntityController<E> : CSDFrameworkPMSPatch.CSDController where E : class, new()
{
    [NonAction]
    public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.Where(predicate);
    }

    [NonAction]
    public IQueryable<TResult> Select<T, TResult>(Expression<Func<T, TResult>> selector) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.Select(selector);
    }

    [NonAction]
    public IQueryable<T> OrderBy<T, TKey>(Expression<Func<T, TKey>> keySelector) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.OrderBy(keySelector);
    }

    [NonAction]
    public IQueryable<T> OrderByDescending<T, TKey>(Expression<Func<T, TKey>> keySelector) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.OrderByDescending(keySelector);
    }

    [NonAction]
    public IQueryable<IGrouping<TKey, T>> GroupBy<T, TKey>(Expression<Func<T, TKey>> keySelector) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.GroupBy(keySelector);
    }

    [NonAction]
    public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.FirstOrDefault(predicate);
    }

    [NonAction]
    public IQueryable<T> Skip<T>(int count) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.Skip(count);
    }

    [NonAction]
    public IQueryable<T> Take<T>(int count) where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.Take(count);
    }

    [NonAction]
    public IQueryable<T> ToList<T>() where T : class, new()
    {
        DbSet<T> dbSet = ctx.Set<T>();
        return dbSet.ToList().AsQueryable();
    }

    [NonAction]
    public T Add<T>(T item) where T : class, new()
    {
        ctx.Set<T>().Add(item);
        ctx.SaveChanges();
        return item;
    }

    [NonAction]
    public T Update<T>(T item) where T : class, new()
    {
        IQueryable<T> query = ctx.Set<T>();
        DbSet<T> dbSet = (DbSet<T>)query;

        var res = dbSet.Find(item);
        ctx.Entry(item).CurrentValues.SetValues(item);
        ctx.SaveChanges();

        return res;
    }

    [NonAction]
    public bool Remove<T>(T req) where T : class, new()
    {
        var res = new Response<T>(); //FIX: Throw error
        IQueryable<T> query = ctx.Set<T>();
        DbSet<T> dbSet = (DbSet<T>)query;

        res.item = dbSet.Find(req);

        ctx.Set<T>().Remove(res.item);
        ctx.SaveChanges();

        return true;
    }

    [NonAction]
    public int SaveChanges() => ctx.SaveChanges();
}
