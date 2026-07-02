using System;
using System.Linq;
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

        //Injectables.Delete<SCA_RISSTRU>((item, context) =>
        //{
        //    TBBASE_H22Context ctx = context.ctx;
        //    var res = ctx.SCA_RISSTRU_ENTITA.Where(x => x.ID_RISTRU == item.ID_RISTRU);
        //    ctx.SCA_RISSTRU_ENTITA.RemoveRange(res);
        //});

        Injectables.LogicalDelete<IModifiable>((item, context) => item.ModDate = DateTime.Now);
        Injectables.LogicalDelete<ISoftDeletable>((item, context) => item.DeletedAt = DateTime.Now);
        Injectables.LogicalDelete<IModUser>((item, context) => item.ModUser = context.CSDContext?.user?.CodiceUtente);
    }
}