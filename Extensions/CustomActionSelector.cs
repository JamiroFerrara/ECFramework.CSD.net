using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace ECFramework;

public class CustomActionSelector : ActionSelector, IActionSelector
{
    readonly IEnumerable<ActionDescriptor> _actions;
    public CustomActionSelector(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        ActionConstraintCache actionConstraintCache, ILoggerFactory loggerFactory)
        : base(actionDescriptorCollectionProvider, actionConstraintCache, loggerFactory)
    {
        _actions = actionDescriptorCollectionProvider.ActionDescriptors.Items;
    }

    ActionDescriptor IActionSelector.SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates)
    {
        // Custom logic to prioritize one method over the other
        if (candidates.Count != 1)
        {
            var customAction = candidates.FirstOrDefault(a => !a.DisplayName.Contains("_"));
            if (customAction != null)
                return customAction; // Give priority to this action
        }

        // Fallback to default behavior if no specific action is found
        return this.SelectBestCandidate(context, candidates);
    }
}
