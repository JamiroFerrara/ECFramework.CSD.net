using System;
using System.Collections.Generic;
using System.Linq;
using CSD.Framework.NetCore.Service.Classes;

namespace ECFramework;

//Used to decode string query parameters in type safe dictionary
public enum RequestParams
{
    Item,
    Items,
    Equals,
    Like,
    Page,
    PageSize,
    OrderBy,
}

public interface IRequest
{
    public Dictionary<string, object> Like { get; set; }
    public Dictionary<string, object> Expressions { get; set; }
    public int Page { get; set; }
    public int? PageSize { get; set; }
}

public class Request<E> : CSDRequest, IRequest, IKeyable where E : IKeyable
{
    public E flatten { get; set; } //NOTE: this is the flattened object that gets removed in client generation.

    //Used in query string parameters for comparison operations
    public new E Equals { get; set; }
    public Dictionary<string, object> Like { get; set; }

    public Dictionary<string, object> Expressions { get; set; }

    //Used in query string parameters for pagination
    public int Page { get; set; }
    public int? PageSize { get; set; }

    //Used in query string parameters for ordering operations
    public string OrderBy { get; set; }

    //Used in Excel export operations to define tabular schema
    public List<(string Field, string Alias)>? Schema { get; set; }

    //Used in update operations to update single/multiple items
    public E Item { get; set; }
    public List<E> Items { get; set; }

    public object[] GetKeys()
    {
        if (Like != null) //This might be useless since i can parse the where clause before hand.
            //NOTE: Conversion mechanism since ASP sometimes converts incorrectly
            return Like.Values.Select(value =>
            {
                if (value is long)
                    return Convert.ToInt32(value);
                if (value is string stringValue && Guid.TryParse(stringValue, out Guid guidValue))
                    return guidValue;
                return value;
            }).ToArray();

        else if (Item != null)
            return Item.GetKeys();
        else
            return [];
    }
}

