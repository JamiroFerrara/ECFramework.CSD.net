using System;
using System.Linq;

[AttributeUsage(AttributeTargets.Property)]
public class ExcelAlias : Attribute
{
    public string Alias { get; }

    public ExcelAlias(string alias)
    {
        Alias = alias;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ExcelOmit : Attribute
{
    public ExcelOmit() { }
}

[AttributeUsage(AttributeTargets.Property)]
public class ExcelNested : Attribute
{
    public string[] Mappings { get; } // Format: "Property=Alias"

    /// <summary>
    /// This uses = sign to split name and alias
    /// </summary>
    public ExcelNested(params string[] mappings)
    {
        Mappings = mappings;
    }
}
