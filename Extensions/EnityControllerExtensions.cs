using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

//NOTE: Utility methods for CSD Crud controller 
public partial class EntityController<E> where E : class, new()
{
    [HttpGet("GetItem")]
    public virtual async Task<ActionResult<Response<E>>> _GetItem([FromQuery] Request<E> req)
        => NoBodyIfCached(await GetItem(req, query => query));

    [HttpGet("GetPage")]
    public virtual async Task<ActionResult<Response<E>>> _GetPage([FromQuery] Request<E> req)
        => NoBodyIfCached(await GetPage(req, query => query));

    [HttpGet("GetExcel")]
    public virtual async Task<Response<E>> _GetExcel([FromQuery] Request<E> req) => await GetExcel(req, query => query);

    [HttpPost("Create")]
    public virtual async Task<Response<E>> _Create([FromBody] List<E> items) => await Create(items, new Request<E>(), query => query);

    [HttpPatch("Update")]
    public async Task<Response<E>> _Update([FromBody] Request<E> req) => await Update(req, query => query);

    [HttpPost("Upsert")]
    public virtual async Task<Response<E>> _Upsert([FromBody] E item) => await Upsert(item, query => query);

    [HttpDelete("Delete")]
    public virtual async Task<Response<E>> _Delete([FromQuery] Request<E> req, [FromBody] List<E>? items = null) { req.Items = items; return await Delete(req, query => query); }

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
        else if (type == typeof(Guid?) && Guid.TryParse(value.ToString(), out Guid guidNullable))
        {
            return Expression.Equal(propertyAccess, Expression.Constant(guidNullable, typeof(Guid?)));
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
            if (equals || equality_only)
            {
                var equalsMethod = typeof(string).GetMethod("Equals", new[] { typeof(string) });

                return Expression.Call(propertyAccess, equalsMethod, Expression.Constant(stringValue));
            }

            //NOTE: Searches are always case-insensitive. EF translates
            //lower(x).Contains(lower(v)) to strpos(lower(x), lower(v)) > 0,
            //which matches regardless of the stored casing.
            var toLower = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var loweredProperty = Expression.Call(propertyAccess, toLower);
            var loweredValue = Expression.Constant(stringValue.ToLower());

            return Expression.Call(loweredProperty, containsMethod, loweredValue);
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
            // Get all navigation properties (i.e., relationships) for the entity.
            // Shadow navigations have no CLR property: nothing can read them, they
            // never reach the JSON, and a split query cannot materialise their
            // collection (EF throws a NullReferenceException in its
            // collection accessor), so they are left out of the include set.
            var navigations = entityType.GetNavigations();

            foreach (var navigation in navigations)
                if (navigation.PropertyInfo != null)
                    query = query.Include(navigation.Name);

            // Many-to-many skip navigations are not returned by GetNavigations()
            foreach (var skip in entityType.GetSkipNavigations())
                if (skip.PropertyInfo != null)
                    query = query.Include(skip.Name);
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
    public Guid? GetEntityId(E item)
    {
        if (item == null) return null;

        var idProperty = item.GetType().GetProperty("Id");
        if (idProperty == null) return null;

        var value = idProperty.GetValue(item);
        if (value is Guid guid) return guid;
        if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    // ── Catalogue response caching ────────────────────────────────────
    // Public catalogue reads change seldom and are the hot path, so they are
    // cacheable for a minute at the edge and revalidate for ten more. Only the
    // controllers whose rows are public catalogue data take part: User, Cart
    // and the other per-session entities stay uncached, and so does anything on
    // an admin or credential-carrying route.
    private const string CatalogueCacheControl = "public, max-age=60, stale-while-revalidate=600";

    private static readonly HashSet<string> CatalogueControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Asset", "Audio", "Artist", "Entity", "Event", "Hero", "Picture",
        "Rig", "SamplePack", "SectionVisibility", "Tag", "Tutorial", "Video", "VinylRelease",
    };

    [NonAction]
    private bool CanCacheCatalogueRead()
    {
        if (ControllerContext?.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return false;

        // Only the two plain reads: GetExcel and custom actions keep their own
        // response semantics.
        if (descriptor.ActionName != nameof(_GetPage) && descriptor.ActionName != nameof(_GetItem))
            return false;

        if (!CatalogueControllers.Contains(descriptor.ControllerName))
            return false;

        // A credential-bearing request is not anonymous — the admin and account
        // views hit the same controllers with a Clerk token attached — so it is
        // never marked publicly cacheable.
        if (Request.Headers.ContainsKey("Authorization") || Request.Headers.ContainsKey("X-Clerk-Id"))
            return false;

        var path = Request.Path.Value ?? string.Empty;
        if (path.Contains("admin", StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var key in Request.Query.Keys)
            if (key.Contains("token", StringComparison.OrdinalIgnoreCase))
                return false;

        return true;
    }

    /// <summary>
    /// Writes the catalogue freshness headers and reports whether the caller's
    /// `If-None-Match` already holds this representation (then the response is
    /// answered 304 and carries no body).
    /// </summary>
    [NonAction]
    private bool WriteCatalogueCacheHeaders(string etag)
    {
        Response.Headers["Cache-Control"] = CatalogueCacheControl;
        // The cache decision itself depends on the credentials, so a proxy must
        // keep authenticated requests out of the anonymous entry.
        Response.Headers["Vary"] = "Authorization, X-Clerk-Id";
        Response.Headers["ETag"] = etag;

        var ifNoneMatch = Request.Headers["If-None-Match"].ToString();
        if (string.IsNullOrEmpty(ifNoneMatch))
            return false;

        var matched = ifNoneMatch
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(tag => tag == "*" || string.Equals(tag, etag, StringComparison.Ordinal));

        if (matched)
            Response.StatusCode = StatusCodes.Status304NotModified;

        return matched;
    }

    /// <summary>
    /// A 304 must not carry the response object: the action results hand back a
    /// bodiless status instead.
    /// </summary>
    [NonAction]
    private ActionResult<Response<E>> NoBodyIfCached(Response<E> res)
    {
        if (Response.StatusCode == StatusCodes.Status304NotModified)
            return StatusCode(StatusCodes.Status304NotModified);

        return res;
    }

    [NonAction]
    private static string BuildCatalogueEtag(int count, DateTime? updated, Guid? id = null)
    {
        var raw = updated.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"{count}:{updated.Value.Ticks}")
            : count.ToString(CultureInfo.InvariantCulture);

        if (id.HasValue)
            raw = string.Create(CultureInfo.InvariantCulture, $"{raw}:{id.Value}");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"\"{Convert.ToHexString(hash)}\"";
    }

    [NonAction]
    private static async Task<string> BuildCatalogueEtagAsync(IQueryable<E> query, int count, CancellationToken token)
        => BuildCatalogueEtag(count, await MaxUpdatedAsync(query, token));

    /// <summary>
    /// The newest timestamp on the queried set, used as the freshness half of
    /// the ETag. AUDIO and PICTURE carry none, so those entities fall back to
    /// the row count.
    /// </summary>
    [NonAction]
    private static async Task<DateTime?> MaxUpdatedAsync(IQueryable<E> query, CancellationToken token)
    {
        var property = TimestampProperty(typeof(E));
        if (property == null)
            return null;

        var parameter = Expression.Parameter(typeof(E), "e");
        Expression access = Expression.Property(parameter, property);
        if (access.Type != typeof(DateTime?))
            access = Expression.Convert(access, typeof(DateTime?));

        var selector = Expression.Lambda<Func<E, DateTime?>>(access, parameter);
        return await query.Select(selector).MaxAsync(token);
    }

    [NonAction]
    private static DateTime? UpdatedTimestampOf(object? item)
    {
        if (item == null)
            return null;

        var property = TimestampProperty(item.GetType());
        return property?.GetValue(item) as DateTime?;
    }

    [NonAction]
    private static PropertyInfo? TimestampProperty(Type type)
    {
        var property = type.GetProperty("UpdatedAt")
            ?? type.GetProperty("ModDate")
            ?? type.GetProperty("CreatedAt");

        if (property == null)
            return null;

        return property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?)
            ? property
            : null;
    }
}

