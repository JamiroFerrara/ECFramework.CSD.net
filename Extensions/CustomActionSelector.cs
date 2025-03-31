using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class RequestBodyEndpointSelector : EndpointSelector
{
    readonly IEnumerable<Endpoint> _controllerEndPoints;
    readonly EndpointSelector _defaultSelector;
    public RequestBodyEndpointSelector(EndpointSelector defaultSelector, EndpointDataSource endpointDataSource)
    {
        _defaultSelector = defaultSelector;
        _controllerEndPoints = endpointDataSource.Endpoints
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() != null).ToList();
    }

    public override async Task SelectAsync(HttpContext httpContext, CandidateSet candidates)
    {
        // Custom logic for selecting the endpoint
        if (candidates.Count == 2)
        {
            // Get the candidate state at index i
            var candidate0 = candidates[0];
            var candidate1 = candidates[1];

            // Assuming that the candidate has an associated Endpoint, we check its DisplayName
            if (candidate0.Endpoint != null && candidate0.Endpoint.DisplayName != null && !candidate0.Endpoint.DisplayName.Contains("_"))
                candidates.SetValidity(0, true); // Mark the current candidate as valid
            else
                candidates.SetValidity(0, false); // Mark the current candidate as valid

            if (candidate1.Endpoint != null && candidate1.Endpoint.DisplayName != null && !candidate1.Endpoint.DisplayName.Contains("_"))
                candidates.SetValidity(1, true); // Mark the current candidate as valid
            else
                candidates.SetValidity(1, false); // Mark the current candidate as valid

            await _defaultSelector.SelectAsync(httpContext, candidates);
            var selectedEndpoint = httpContext.GetEndpoint();
            return;
        }

        // Fallback to default behavior if no specific action is found
        await _defaultSelector.SelectAsync(httpContext, candidates);
    }
}

//define an extension method for registering conveniently
public static class EndpointSelectorServiceCollectionExtensions
{
    public static IServiceCollection AddRequestBodyEndpointSelector(this IServiceCollection services)
    {
        //build a dummy service container to get an instance of 
        //the DefaultEndpointSelector
        var sc = new ServiceCollection();
        sc.AddMvc();
        var defaultEndpointSelector = sc.BuildServiceProvider().GetRequiredService<EndpointSelector>();
        return services.Replace(new ServiceDescriptor(typeof(EndpointSelector),
                                sp => new RequestBodyEndpointSelector(defaultEndpointSelector,
                                                                      sp.GetRequiredService<EndpointDataSource>()),
                                ServiceLifetime.Singleton));
    }
}
