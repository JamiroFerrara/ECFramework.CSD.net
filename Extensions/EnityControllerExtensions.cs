using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CSD.Framework.NetCore.DataAccessLayer.Entities;
using CSD.Framework.NetCore.Service.Classes;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace ECFramework;

//NOTE: Utility methods for CSD Crud controller 
public partial class EntityController<E> : CSDFrameworkPMSPatch.CSDController where E : class, new()
{
    [HttpGet("GetItem")]
    public virtual async Task<Response<E>> _GetItem([FromQuery] Request<E> req) => await GetItem(req, query => query);

    [HttpGet("GetPage")]
    public virtual async Task<Response<E>> _GetPage([FromQuery] Request<E> req) => await GetPage(req, query => query);

    [HttpGet("GetExcel")]
    public virtual async Task<Response<E>> _GetExcel([FromQuery] Request<E> req) => await GetExcel(req, query => query);

    [HttpPost("Create")]
    public virtual async Task<Response<E>> _Create([FromBody] List<E> items) => await Create(items, new Request<E>(), query => query);

    [HttpPatch("Update")]
    public async Task<Response<E>> _Update([FromBody] Request<E> req) => await Update(req, query => query);

    [HttpDelete("Delete")]
    public virtual async Task<Response<E>> _Delete([FromQuery] Request<E> req, [FromBody] List<E>? items = null) { req.Items = items; return await Delete(req, query => query); }

    [NonAction] //NOTE: CSDPermisson handling 
    public async Task<R> Try<R>(Func<List<string>, Task<R>> action, Permissions permission) where R : CSDResponse, new()
    {
        var res = new R();
        //Error handling is in this try extension
        return await Try<R>(async () =>
        {
            if (Debug.IgnorePermissions) //TODO: Inject fake user in cookies here
                return await action(new List<string>());

            return await CSDAuthRead<R>(async actions =>
            {
                if (permission == Permissions.Read)
                    if (CanRead(actions))
                        return await action(actions);
                    else
                        res.SetResponse(new CSDResponse { Rc = 11, RcDescription = Permissions.Read.GetDescription() });
                if (permission == Permissions.Write)
                    if (CanRead(actions) && CanWrite(actions))
                        return await action(actions);
                    else
                        res.SetResponse(new CSDResponse { Rc = 12, RcDescription = Permissions.Write.GetDescription() });
                return res;
            });
        });
    }

    [NonAction]
    public static IQueryable<E> ApplyExpressionTree<E>(IQueryable<E> query, string key, List<ExpressionNode> expressions, PropertyInfo? sub_property, string sub_key, bool equality_only)
    {
        if (expressions == null || expressions.Count == 0)
            return query;

        var parameter = Expression.Parameter(typeof(E), "e");
        Expression finalExpression = null;

        foreach (var expr in expressions)
        {
            var exp = BuildExpression<E>(expr, key, parameter, sub_property, sub_key, equality_only);
            if (exp == null) continue;

            switch (expr)
            {
                case UnaryExpression unaryExpr:
                    switch (unaryExpr.Operator)
                    {
                        case TokenType.Contains:
                            finalExpression = finalExpression == null ? exp : Expression.And(finalExpression, exp);
                            break;
                        case TokenType.Or:
                            finalExpression = finalExpression == null ? exp : Expression.Or(finalExpression, exp);
                            break;
                        case TokenType.And:
                            finalExpression = finalExpression == null ? exp : Expression.And(finalExpression, exp);
                            break;
                        case TokenType.Not:
                            finalExpression = finalExpression == null ? exp : Expression.Not(exp);
                            break;
                        default:
                            break;
                    }
                    break;
                case IdentifierExpression idExpr:
                    finalExpression = exp;
                    break;
            }
        }

        if (finalExpression == null)
            return query;

        var lambda = Expression.Lambda<Func<E, bool>>(finalExpression, parameter);
        return query.Where(lambda);
    }

    [NonAction]
    private static Expression BuildExpression<E>(ExpressionNode node, string key, ParameterExpression parameter, PropertyInfo? sub_property, string sub_key, bool equality_only)
    {
        var current_key = sub_key == "" ? key : sub_key;
        var entityType = typeof(E);
        var property = entityType.GetProperty(current_key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
            throw new ArgumentException($"Property '{key}' not found on type '{entityType.Name}'");

        Expression propertyAccess = Expression.Property(parameter, property);

        if (sub_property != null)
        {
            var sub_key_property = sub_property.PropertyType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            propertyAccess = Expression.Property(propertyAccess, sub_key_property);
        }

        switch (node)
        {
            case IdentifierExpression idExpr:
                return MatchExpression(propertyAccess, idExpr.Value, true, key, equality_only);
            case UnaryExpression unaryExpr:
                var operand = BuildExpression<E>(unaryExpr.Operand, key, parameter, sub_property, sub_key, equality_only);
                if (operand == null) return null;

                return unaryExpr.Operator switch
                {
                    TokenType.Contains => MatchExpression(propertyAccess, ((IdentifierExpression)unaryExpr.Operand).Value, false, key, equality_only),
                    TokenType.Not => Expression.Not(operand),
                    _ => operand
                };
        }

        return null;
    }

    [NonAction]
    private static Expression MatchExpression(Expression propertyAccess, object value, bool equals, string key, bool equality_only)
    {
        var type = propertyAccess.Type;

        // Handle null and !null cases
        if (value.ToString().ToLower() == "null")
            return Expression.Equal(propertyAccess, Expression.Constant(null));
        else if (value.ToString().ToLower() == "!null")
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));

        if (type == typeof(Guid) && Guid.TryParse(value.ToString(), out Guid guid))
        {
            var method = type.GetMethod("Equals", new[] { typeof(Guid) });
            return Expression.Call(propertyAccess, method, Expression.Constant(guid));
        }
        else if (type == typeof(DateTime) || type == typeof(DateTime?))
        {
            if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
            {
                var propertyDateAccess = type == typeof(DateTime?)
                    ? Expression.Property(Expression.Property(propertyAccess, "Value"), "Date")
                    : Expression.Property(propertyAccess, "Date");

                var method = propertyDateAccess.Type.GetMethod("Equals", new[] { typeof(DateTime) });
                return Expression.Call(propertyDateAccess, method, Expression.Constant(dateValue.Date));
            }
        }
        else if (type == typeof(double) && double.TryParse(value.ToString(), out double doubleValue))
        {
            return Expression.Equal(propertyAccess, Expression.Constant(doubleValue, typeof(double)));
        }
        else if (type == typeof(int) && int.TryParse(value.ToString(), out int intValue))
        {
            return Expression.Equal(propertyAccess, Expression.Constant(intValue, typeof(int)));
        }
        else if (type == typeof(string) && value is string stringValue)
        {
            var method = typeof(string).GetMethod(equals ? "Equals" : "Contains", new[] { typeof(string) });
            if (equality_only)
                method = typeof(string).GetMethod("Equals", new[] { typeof(string) });

            return Expression.Call(propertyAccess, method, Expression.Constant(stringValue));
        }
        else
        {
            try
            {
                value = Convert.ChangeType(value, type);
                var constant = Expression.Constant(value, type);
                return Expression.Equal(propertyAccess, constant);
            }
            catch (Exception)
            {
                return Expression.Empty();
            }
        }

        return Expression.Empty();
    }

    [NonAction]
    public IQueryable<E> ApplyWhere(IQueryable<E> query, Dictionary<string, object> filters, PropertyInfo? sub_property, string sub_key, bool equality_only = false)
    {
        if (sub_property == null)
            filters = filters.Where(f => typeof(E).GetProperties().ToList().Any(p => p.Name.ToLower() == f.Key.ToLower())).ToDictionary(d => d.Key, d => d.Value);
        else
            filters = filters.Where(f => sub_property.PropertyType.GetProperties().ToList().Any(p => p.Name.ToLower() == f.Key.ToLower())).ToDictionary(d => d.Key, d => d.Value);

        foreach (var filter in filters ?? new())
        {
            if (filter.Value is List<ExpressionNode> expressions)
            {
                query = ApplyExpressionTree(query, filter.Key, expressions, sub_property, sub_key, equality_only);
                continue;
            }
            else if (filter.Value is Dictionary<string, object> subFilters)
            {
                var type = typeof(E);
                var prop = type.GetProperty(filter.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                query = ApplyWhere(query, subFilters, prop, filter.Key, equality_only);
                continue;
            }
        }

        return query;
    }

    [NonAction]
    public static IQueryable<E> ApplyIncludes(IQueryable<E> query, DbContext context)
    {
        // Get the entity type metadata using the CLR type of E
        var entityType = context.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType == typeof(E));

        if (entityType != null)
        {
            // Get all navigation properties (i.e., relationships) for the entity
            var navigations = entityType.GetNavigations();

            foreach (var navigation in navigations)
                query = query.Include(navigation.Name);
        }

        return query;
    }

    [NonAction]
    public static IQueryable<E> ApplyOrderBy(IQueryable<E> query, string orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return query;

        var entityType = typeof(E);
        var parameter = Expression.Parameter(entityType, "e");

        var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var propertyPath = parts[0]; // The full property path, e.g., "Parent.Child.Property"
        var direction = parts.Length == 1 ? "asc" : parts[parts.Length - 1].ToLower(); // Default to "asc" if no direction is provided

        // Resolve the nested property
        Expression propertyAccess = parameter;
        Type currentType = entityType;

        foreach (var propertyName in propertyPath.Split('.'))
        {
            var property = currentType.GetProperty(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{currentType.Name}'.");

            propertyAccess = Expression.Property(propertyAccess, property);
            currentType = property.PropertyType;
        }

        // Determine the method to use based on the direction
        string methodName = direction switch
        {
            "asc" => "OrderBy",
            "desc" => "OrderByDescending",
            _ => throw new ArgumentException("Invalid sort direction. Expected 'asc' or 'desc'.")
        };

        // Create the order by expression
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);
        var orderByMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2);

        var genericMethod = orderByMethod.MakeGenericMethod(entityType, currentType);
        query = (IQueryable<E>)genericMethod.Invoke(null, new object[] { query, orderByExpression });

        return query;
    }

    [NonAction]
    private List<E> CheckReflectiveId(List<E> items)
    {
        foreach (var item in items)
            CheckReflectiveId(item);
        return items;
    }

    [NonAction]
    public E CheckReflectiveId(E item)
    {
        //Single value
        if (item != null)
        {
            // Set Id using reflection, handling both Guid and string types
            var idProperty = item.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(item);

                // If the Id is a Guid and is empty, set a new Guid
                if (idValue is Guid id && id == Guid.Empty)
                {
                    if (idProperty.PropertyType == typeof(Guid))
                        idProperty.SetValue(item, Guid.NewGuid());

                    else if (idProperty.PropertyType == typeof(string))
                        idProperty.SetValue(item, Guid.NewGuid().ToString()); // Convert Guid to string before setting
                }
                // If the Id is a string and parses to an empty Guid, set a new Guid
                else if (idValue is string idStr && Guid.TryParse(idStr, out Guid parsedId) && parsedId == Guid.Empty)
                {
                    if (idProperty.PropertyType == typeof(string))
                        idProperty.SetValue(item, Guid.NewGuid().ToString());

                    else if (idProperty.PropertyType == typeof(Guid))
                        idProperty.SetValue(item, Guid.NewGuid());
                }
            }
        }


        return item;
    }

    [NonAction]
    private void MultiUpdate(DbContext ctx, DbSet<E> dbSet, dynamic req)
    {
        for (int i = 0; i < req.Items.Count; i++)
        {
            var items = req.Where["Items"] as JArray;
            var list_items = items.Select(token => token.ToObject<E>()).ToList();
            if (list_items != null)
            {
                var item = list_items[i];
                var found_item = dbSet.Find(item);
                ctx.Entry(found_item).CurrentValues.SetValues(req.Items[i]);

                //Set the ModDate for all items
                foreach (var element in list_items)
                    Injectables.RunUpdate(element, this);

                ctx.SaveChanges();
            }
        }
    }
}
