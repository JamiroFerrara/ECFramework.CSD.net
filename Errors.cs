using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

//NOTE: Error routing goes here
public partial class EntityController<E> : Controller where E : class, new()
{
[NonAction] //TODO: This should become async by default
    public async Task<R> Try<R>(Func<Task<R>> action) where R : Response<E>, new()
    {
        var res = new R();
        try { return await action(); }

        catch (Exception e)
        {
            res.error = e.Message + ":" + e.InnerException;
            Injectables.RunError(e, this);
            return res;
        }
    }
}
