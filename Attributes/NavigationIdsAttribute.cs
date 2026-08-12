using System;

/// <summary>
/// Marks a [NotMapped] comma-joined string of Guid ids (e.g. "ArtistIds") and
/// names the collection navigation it hydrates (e.g. "Artists"). The framework
/// attaches the referenced entities by key on create/upload — no per-entity
/// injectable needed.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NavigationIdsAttribute : Attribute
{
    public string Navigation { get; }

    public NavigationIdsAttribute(string navigation)
    {
        Navigation = navigation;
    }
}
