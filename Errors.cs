using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

//NOTE: Error routing goes here
public partial class EntityController<E> : Controller where E : class, new()
{
[NonAction] 
    public async Task<R> Try<R>(Func<Task<R>> action) where R : Response<E>, new()
    {
        var res = new R();
        try { return await action(); }

        catch (Exception e)
        {
            // An error body must never be stored or revalidated: the catalogue
            // ETag is derived from (count, updated) and would otherwise be the
            // same as the successful body's, so a client could revalidate a
            // failed response and be answered 304 for a body it never received.
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers.Remove("ETag");
            Response.Headers.Remove("Vary");

            res.error = e.Message + ":" + e.InnerException;
            Injectables.RunError(e, this);
            return res;
        }
    }
}
