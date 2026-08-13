using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Generic handling for many-to-many (and any "ids -> navigation")
/// relationships. Any [NotMapped] string property tagged with
/// [NavigationIds("Nav")] (comma-joined Guid ids) maps to the named collection
/// navigation. Runs before SaveChanges in Create/Upload/Update, so no
/// per-entity injectable is required.
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

            var nav = item.GetType().GetProperty(attr.Navigation);
            if (nav == null)
                continue;

            var collection = nav.GetValue(item) as IList;
            if (collection == null)
                continue;

            // Rebuild from the string — clear first so a deserialized (stale)
            // collection never leaks through, and an empty string removes all.
            collection.Clear();

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

            var elementType = nav.PropertyType.GetGenericArguments().FirstOrDefault();
            if (elementType == null)
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
    /// Syncs [NavigationIds] collection navigations onto the tracked target by
    /// diffing against the source's comma-joined id string — removing dropped
    /// associations and adding new ones. Diffing (rather than clear + re-add)
    /// avoids duplicate-key collisions on the join table when an association is
    /// unchanged.
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

            var targetCollection = nav.GetValue(target) as IList;
            if (targetCollection == null)
                continue;

            var raw = prop.GetValue(source) as string;
            var ids = (raw ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g.Value)
                .Distinct()
                .ToList();

            var elementType = nav.PropertyType.GetGenericArguments().FirstOrDefault();

            // Remove associations no longer present.
            foreach (var existing in targetCollection.Cast<object>().ToList())
            {
                if (!ids.Contains(GetEntityKey(existing)))
                    targetCollection.Remove(existing);
            }

            // Add associations that are missing.
            if (elementType == null)
                continue;

            var existingKeys = targetCollection.Cast<object>()
                .Select(GetEntityKey)
                .ToHashSet();

            foreach (var id in ids)
            {
                if (existingKeys.Contains(id))
                    continue;

                var entity = ctx.Find(elementType, id);
                if (entity != null)
                {
                    targetCollection.Add(entity);
                    existingKeys.Add(id);
                }
            }
        }
    }

    /// <summary>
    /// Populates [NavigationIds] string properties (comma-joined Guid ids) from
    /// the loaded collection navigation, so read responses carry the ids the
    /// form needs to pre-select. Symmetric to HydrateNavigationIds (string →
    /// collection), which runs on create/update/upload.
    /// </summary>
    [NonAction]
    public void HydrateNavigationIdsString(object item)
    {
        if (item == null)
            return;

        foreach (var prop in item.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<NavigationIdsAttribute>();
            if (attr == null)
                continue;

            var nav = item.GetType().GetProperty(attr.Navigation);
            if (nav == null)
                continue;

            var collection = nav.GetValue(item) as IList;
            if (collection == null)
                continue;

            var ids = new List<string>();
            foreach (var entity in collection)
            {
                var idProp = entity?.GetType().GetProperty("Id");
                var idValue = idProp?.GetValue(entity);
                if (idValue != null)
                    ids.Add(idValue.ToString());
            }

            prop.SetValue(item, string.Join(",", ids));
        }
    }

    [NonAction]
    private static Guid GetEntityKey(object entity)
    {
        var idProp = entity?.GetType().GetProperty("Id");
        var value = idProp?.GetValue(entity);
        if (value is Guid guid)
            return guid;
        if (value is string s && Guid.TryParse(s, out var parsed))
            return parsed;
        return Guid.Empty;
    }

    /// <summary>
    /// Loads all navigation collections on a tracked entity so that M2M sync can
    /// diff against the current associations rather than re-adding them.
    /// </summary>
    [NonAction]
    public void LoadNavigations(object entity)
    {
        if (entity == null)
            return;

        foreach (var nav in ctx.Entry(entity).Navigations)
        {
            // Shadow navigations (the inverse side of a unidirectional M2M)
            // have no CLR member and cannot be loaded via Load(); skip them.
            if (nav.Metadata.PropertyInfo == null && nav.Metadata.FieldInfo == null)
                continue;

            if (!nav.IsLoaded)
                nav.Load();
        }
    }
}
