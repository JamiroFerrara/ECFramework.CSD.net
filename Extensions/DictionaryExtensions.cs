using System.Collections.Generic;

namespace ECFramework;

public static class DictionaryExtensions
{
    public static bool TryGetValueAs<T>(this IDictionary<string, object> dict, string key, out T result)
    {
        if (dict.TryGetValue(key, out var value) && value is T tValue)
        {
            result = tValue;
            return true;
        }
        result = default;
        return false;
    }

    public static dynamic TryGet(this IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
            return value;
        return null; // or throw an exception if desired
    }
}
