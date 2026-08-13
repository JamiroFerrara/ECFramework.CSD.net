using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Generic create-time hydration for many-to-many (and any "ids -> navigation")
/// relationships. Any [NotMapped] string property tagged with
/// [NavigationIds("Nav")] (comma-joined Guid ids) has its referenced entities
/// attached by key into the named collection navigation. Runs before SaveChanges
/// in both Create and Upload, so no per-entity injectable is required.
/// </summary>
public partial class EntityController<E> where E : class, new()
{
    [NonAction]
    public void HydrateNavigationIds(object item)
    {
        if (item == null)
            return;

        foreach (var prop in item.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<NavigationIdsAttribute>();
            if (attr == null)
                continue;

            var raw = prop.GetValue(item) as string;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var ids = raw
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                continue;

            var nav = item.GetType().GetProperty(attr.Navigation);
            if (nav == null)
                continue;

            var elementType = nav.PropertyType.GetGenericArguments().FirstOrDefault();
            if (elementType == null)
                continue;

            var collection = nav.GetValue(item) as IList;
            if (collection == null)
                continue;

            foreach (var id in ids)
            {
                var entity = ctx.Find(elementType, id);
                if (entity != null)
                    collection.Add(entity);
            }
        }
    }

    /// <summary>
    /// Copies [NavigationIds] collection navigations from the (deserialized)
    /// source item onto the tracked target, so updates replace the target's
    /// many-to-many associations rather than appending to them.
    /// </summary>
    [NonAction]
    public void SyncNavigationCollections(object source, object target)
    {
        if (source == null || target == null)
            return;

        foreach (var prop in source.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<NavigationIdsAttribute>();
            if (attr == null)
                continue;

            var nav = source.GetType().GetProperty(attr.Navigation);
            if (nav == null)
                continue;

            var sourceCollection = nav.GetValue(source) as IList;
            var targetCollection = nav.GetValue(target) as IList;
            if (sourceCollection == null || targetCollection == null)
                continue;

            targetCollection.Clear();
            foreach (var item in sourceCollection)
                targetCollection.Add(item);
        }
    }
}
