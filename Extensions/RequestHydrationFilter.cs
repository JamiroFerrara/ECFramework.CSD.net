using System;
using System.Web;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECFramework;

public class RequestHydrationFilter : ActionFilterAttribute
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new JsonElementConverter(), new GuidConverter() }
    };

    //NOTE: This function parses query string parameters back into the request
    //object. This is done because GET requests do not allow request bodies, and
    //doesn't play well with complex objects.
    //Why? This allows for much faster API's 50ms > 5ms 10x speed.
    //Also by having the parameters as a query string it allows for 1st level
    //paramters in the typescript-swagger-api client.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.Values.FirstOrDefault() is not IRequest iRequest) return; //Is the first param of type IRequest?
        if (context.Controller is not ControllerBase controller) return; //Controller must be of type controller base

        if (iRequest.PageSize == -1) iRequest.PageSize = 9999999;

        //NOTE: Parses query string values
        var parsed = new List<string>();
        if (!string.IsNullOrEmpty(controller.Request.QueryString.Value))
        {
            iRequest.Like = null; //NOTE: Clear any values
            parsed = controller.Request.QueryString.Value.Remove(0, 1).Split('&').Select(x => HttpUtility.UrlDecode(x)).ToList();

            //NOTE: Parses "Like" dictionary from query string
            string like = parsed.FirstOrDefault(x => x.StartsWith($@"{RequestParams.Like.ToString()}="));

            //Remove request elements that are not part of the query
            parsed.RemoveAll(x =>
                    x.StartsWith($@"{RequestParams.Page.ToString()}=") ||
                    x.StartsWith($@"{RequestParams.PageSize.ToString()}=") ||
                    x.StartsWith($@"{RequestParams.Like.ToString()}=") ||
                    x.StartsWith($@"{RequestParams.OrderBy.ToString()}="));

            if (!string.IsNullOrEmpty(like))
                iRequest.Like = JsonSerializer.Deserialize<Dictionary<string, object>>(like.Replace($@"{RequestParams.Like.ToString()}=", ""), _jsonOptions);
        }

        //NOTE: Parses equality values from query string and adds them to where dictionary.
        var where = new Dictionary<string, object>();
        foreach (var item in iRequest.Like ?? new())
        {
            if (where.ContainsKey(item.Key)) continue; // Skip if key already exists
            if (item.Value == null) continue; // Skip if key already exists

            string expression = item.Value.ToString();
            var tree = ParseTree("%" + expression); //FIX: This will also need to change probably

            where.Add(item.Key, tree);
        };

        foreach (var item in parsed)
        {
            var key_value = item.Split('=', 2);
            if (key_value.Length != 2) continue; // Ensures we have exactly 2 parts: key and value

            string prefix = "";
            string expression = key_value[1];
            if (expression == "%") continue; // Skip if expression is just a percent sign
            var tree = ParseTree(expression);

            string key = prefix + key_value[0];

            //NOTE: Parse sub_objects 1 level deep
            //TODO: Pull this out to recursive function?
            if (expression.StartsWith('{'))
            {
                var sub_object = JsonSerializer.Deserialize<Dictionary<string, object>>(expression, _jsonOptions);
                var new_sub_object = new Dictionary<string, object>();
                foreach (var sub_item in sub_object)
                {
                    var sub_expression = sub_item.Value.ToString();
                    new_sub_object.Add(sub_item.Key, ParseTree(sub_expression));
                }

                where.Add(key, new_sub_object);
                continue;
            }

            if (where.ContainsKey(key)) continue; // Skip if key already exists
            where.Add(key, tree);
        };

        iRequest.Expressions = where;
    }

    private List<ExpressionNode> ParseTree(string expression)
    {
        List<Token> tokens = new();
        List<ExpressionNode> tree = new();
        {
            tokens = ExpressionTokenizer.Tokenize(expression);
            tree = new ExpressionParser(tokens).ParseExpression();
        }
        return tree;
    }
}
