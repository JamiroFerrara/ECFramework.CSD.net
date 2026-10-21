using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECFramework;

/// <summary>
/// Rewrites the expanded query names ApiExplorer reports for <c>Request&lt;E&gt;</c>
/// (<c>flatten.Id</c>, <c>Equals.Id</c>, …) into the bare vocabulary <c>RequestHydrationFilter</c>
/// actually reads, dropping the parameters nothing reads. Left as they are, a generated client
/// would send filters no route honours.
/// </summary>
public class QueryParameterOperationFilter : IOperationFilter
{
    /// <summary>The expansion's prefix: the only sign an operation came from `Request&lt;E&gt;`.</summary>
    private const string Expanded = "flatten.";

    /// <summary>The framework's name for the export, the one action that reads the schema.</summary>
    private const string ExcelExport = "GetExcel";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!operation.Parameters.Any(parameter => parameter.Name.StartsWith(Expanded, StringComparison.OrdinalIgnoreCase)))
            return; // Not an entity route: nothing to expand.

        // The export answers the envelope like every other read, the spreadsheet going to the
        // `file` channel, so the operation itself names it.
        var excel = context.MethodInfo.Name.EndsWith(ExcelExport, StringComparison.Ordinal);

        var parameters = new List<OpenApiParameter>();

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Name.StartsWith(Expanded, StringComparison.OrdinalIgnoreCase))
            {
                parameter.Name = parameter.Name[Expanded.Length..];
                parameter.Description = "Filtro sul campo: un valore è un confronto, `%testo` un like.";
                parameters.Add(parameter);
            }
            else if (parameter.Name == "Like")
            {
                // Read as JSON text out of the query string (`RequestHydrationFilter`), which no
                // client can put in a query as an object.
                parameter.Schema = new OpenApiSchema { Type = "string" };
                parameter.Description = "Il dizionario dei filtri, in JSON: `{\"State\":\"failed\"}`.";
                parameters.Add(parameter);
            }
            else if (parameter.Name is "Page" or "PageSize" or "OrderBy" || parameter.Name == "Schema" && excel)
                parameters.Add(parameter);
        }

        operation.Parameters = parameters;
    }
}
