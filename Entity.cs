using System;
using System.ComponentModel.DataAnnotations;

namespace ECFramework;

/// <summary>
/// By implementing this you get automatic ModDate updates from framework
/// </summary>
public interface IModifiable
{
    public DateTime? ModDate { get; set; }
}

public interface ICreateable
{
    public DateTime CreatedAt { get; set; }
}

public interface IModUser
{
    public string ModUser { get; set; }
}

public interface IKeyable
{
    public object[] GetKeys();
}

public class Entity : IKeyable
{
    // public DateTime CreatedAt { get; set; } = DateTime.Now;

    // public DateTime ModDate { get; set; } = DateTime.Now;

    // public DateTime? DeletedAt { get; set; }

    // public string ModUser { get; set; }

    // public string Abi { get; set; }

    // public bool IsDeleted => DeletedAt != null;

    public virtual object[] GetKeys()
    {
        // Cheat using reflection.
        var idProperty = this.GetType().GetProperty("Id");
        if (idProperty != null)
        {
            var idValue = idProperty.GetValue(this);
            if (idValue is Guid guidValue)
                return [guidValue];
        }

        return Array.Empty<object>();
    }
}
