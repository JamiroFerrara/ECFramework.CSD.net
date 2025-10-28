using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSD.Framework.NetCore.Service.Classes;
using CSD.Framework.NetCore.Service.Classes.DataPrivacyCollector;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECFramework;

public partial class EntityController<E> : CSDFrameworkPMSPatch.CSDController
{
    public CSDContext CSDContext;

    [NonAction]
    protected async Task<R> CSDAuthRead<R>(Func<List<string>, Task<R>> action) where R : CSDResponse, new()
    {
        var headers = this.Request.Headers["ctx"].ToString();
        var request = JsonSerializer.Deserialize<CSDContext>(headers);
        this.CSDContext = request;

        if (this.CSDContext == null)
            throw new Exception("Unable to deserialize CSDContext");

        return await Try<R>(() =>
        {
            var serivce = InitSerivce(this.Request, request);
            var actions = GetActions(request);

            var res = action.Invoke(actions);
            return res;
        });
    }

    private DataPrivacyCollectorInterface InitSerivce(HttpRequest httpRequest, CSDContext ctx)
    {
        var service = InitializeService(this.Request, new CSDRequest { ctx = ctx });
        if (service == null)
            throw new UnauthorizedException();

        service.dataPrivacyEntity.ExcludeTracePreLog = true;

        return service;
    }

    private List<string> GetActions(CSDContext ctx)
    {
        string appid = ctx.application.CodApplicazione.ToString();
        List<string> AzioniUtente = new CSDFrameworkPMSPatch.ActionService(ctx).GetActionsForUser(ctx.user.CodiceUtente, ctx.user.CodiceAbiDefault, appid).Select(x => x.Split('|').ToList().Last()).ToList();

        if (!AzioniUtente.Contains("ACCESSO"))
            throw new UnauthorizedException();

        return AzioniUtente;
    }

}
