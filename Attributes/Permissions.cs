using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

public enum Permissions
{
    [Description("User does not have read permissions.")]
    Read = 10,
    [Description("User does not have write permissions.")]
    Write = 11,
}

[AttributeUsage(AttributeTargets.Class)]
public class CSDPermissions : Attribute
{
    public string[] permissions { get; set; }
    public CSDPermissions(string[] permissions)
    {
        this.permissions = permissions;
    }
}

public partial class EntityController<E> : Controller where E : class, new()
{
    [NonAction]
    public string[] GetCSDPermissons()
    {
        var customClassAnnotation = (CSDPermissions)Attribute.GetCustomAttribute(typeof(E), typeof(CSDPermissions));
        if (customClassAnnotation != null)
            return customClassAnnotation.permissions;
        else
            return [];
    }

    [NonAction]
    public string GetRead()
    {
        var permissions = GetCSDPermissons();

        if (permissions.Length > 0)
            return permissions[0]; //Read 

        return "";
    }

    [NonAction]
    public string GetWrite()
    {
        var permissions = GetCSDPermissons();

        if (permissions.Length > 0)
            return permissions[1]; //Write

        return "";
    }

    [NonAction]
    public bool CanRead(List<string> permissions)
    {
        if (Debug.IgnorePermissions)
            return true;

        var read = GetRead();
        if (read == "")
            return true;

        if (permissions.Contains(read))
            return true;
        else
            return false;
    }

    [NonAction]
    public bool CanWrite(List<string> permissions)
    {
        if (Debug.IgnorePermissions)
            return true;

        var write = GetWrite();
        if (write == "")
            return true;

        if (permissions.Contains(write))
            return true;
        else
            return false;
    }
}

public static class Extensions
{
    public static string GetDescription(this Permissions e)
    {
        var attribute =
            e.GetType()
                .GetTypeInfo()
                .GetMember(e.ToString())
                .FirstOrDefault(member => member.MemberType == MemberTypes.Field)
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault()
                as DescriptionAttribute;

        return attribute?.Description ?? e.ToString();
    }
}
