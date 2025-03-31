using System;
using Microsoft.Extensions.DependencyInjection;

namespace ECFramework;

public static class IServiceExtensions
{
    public static void HandleInjectables(this IServiceCollection services)
    {
        Injectables.Create<IModifiable>((item, context) => item.ModDate = DateTime.Now);
        Injectables.Create<ICreateable>((item, context) => item.CreatedAt = DateTime.Now);
        Injectables.Create<IModUser>((item, context) => item.ModUser = context.CSDContext?.user?.CodiceUtente);

        Injectables.Update<IModifiable>((item, context) => item.ModDate = DateTime.Now);
        Injectables.Update<IModUser>((item, context) => item.ModUser = context.CSDContext?.user?.CodiceUtente);

        Injectables.LogicalDelete<IModifiable>((item, context) => item.ModDate = DateTime.Now);
        Injectables.LogicalDelete<ISoftDeletable>((item, context) => item.DeletedAt = DateTime.Now);
        Injectables.LogicalDelete<IModUser>((item, context) => item.ModUser = context.CSDContext?.user?.CodiceUtente);
    }
}
