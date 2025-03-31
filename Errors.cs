using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CSD.Framework.NetCore.Service.Classes;
using CSD.Framework.NetCore.Utility;
using Microsoft.AspNetCore.Mvc;

namespace ECFramework;

public partial class EntityController<E> : CSDFrameworkPMSPatch.CSDController where E : class, new()
{
    [NonAction] //TODO: This should become async by default
    public async Task<R> Try<R>(Func<Task<R>> action) where R : CSDResponse, new()
    {
        var res = new R();
        try { return await action(); }

        catch (Exception e)
        {
            res.SetResponse(ApplicationLogging.LogErrorResponse(MethodBase.GetCurrentMethod().DeclaringType.FullName + "-" + MethodBase.GetCurrentMethod().Name, e));
            res.RcDescription = e.Message;
            res.Rc = -1;
            return res;
        }
    }
}
