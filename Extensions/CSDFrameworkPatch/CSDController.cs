#region Assembly CSD.Framework.NetCore, Version=1.0.74.3786, Culture=neutral, PublicKeyToken=null
// CSD.Framework.NetCore.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CSD.Framework.NetCore.DataAccessLayer.Entities;
using CSD.Framework.NetCore.DataAccessLayer.SqlInterface;
using CSD.Framework.NetCore.Service.Classes;
using CSD.Framework.NetCore.Service.Classes.DataPrivacyCollector;
using CSD.Framework.NetCore.Service.MongoLogger;
using CSD.Framework.NetCore.Service.MongoLogger.Configuration;
using CSD.Framework.NetCore.Service.SqlServices;
using CSD.Framework.NetCore.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CSDFrameworkPMSPatch;

public class CSDController : Controller
{
    private const string COD_PROCEDURA_SIC = "SIC";

    private const string COD_PROCEDURA_SIO = "SIO";

    private const string COD_PROCEDURA_SID = "SID";

    private const string MACRO_AMBITO_CONTROLLI = "SIC";

    private const string MACRO_AMBITO_CUS_PRO = "CUS_PRO";

    private const string MACRO_CONTENUTO = "0";

    private const string MOLTEPLICITA_NO = "0";

    private const string MOLTEPLICITA_CLIENETE = "N";

    private const string MOLTEPLICITA_RAPPORTO = "N";

    private const string MOLTEPLICITA_MOVIM = "N";

    private const string MODULO_QM = "00168";

    private const string MODULO_ANALIZZA = "00167";

    private const string MODULO_DP = "00171";

    private const string MODULO_CUSPRO = "00170";

    private const string MODULO_GRUPRO = "00180";

    private string userinfo;

    private DateTime startTime;

    protected IConfiguration configuration { get; set; }

    protected AppSettings app_settings { get; set; }

    protected CSDController(IConfiguration configuration)
    {
        this.configuration = configuration;
        AppSettings appSettings = new AppSettings();
        configuration.GetSection("AppSettings").Bind(appSettings);
        if (appSettings == null)
        {
            ApplicationLogging.LogError("appsettings.json configuration error\r\n\t => Section AppSettings not found");
        }
        app_settings = appSettings;
        DataPrivacySettings dataPrivacySettings = new DataPrivacySettings();
        configuration.GetSection("AppSettings").GetSection("DataPrivacySettings").Bind(dataPrivacySettings);
        if (dataPrivacySettings == null)
        {
            ApplicationLogging.LogError("appsettings.json configuration error\r\n\t => Section DataPrivacySettings not found");
        }
        app_settings.dp_settings = dataPrivacySettings;
        MongoDB_Settings mongoDB_Settings = new MongoDB_Settings();
        configuration.GetSection("AppSettings").GetSection("MongoDB_ErrlogSettings").Bind(mongoDB_Settings);
        if (mongoDB_Settings == null)
        {
            ApplicationLogging.LogError("appsettings.json configuration error\r\n\t => Section MongoDB_ErrlogSettings not found");
        }
        app_settings.errlog_settings = mongoDB_Settings;
    }

    protected DataPrivacyCollectorInterface InitializeService(HttpRequest httpRequest, CSDRequest req, bool forceNoTokenValidation = false)
    {
        DataPrivacyCollectorInterface dataPrivacyCollectorInterface = null;
        string value = httpRequest.Path.Value;
        string absolutePathOriginalPath = base.HttpContext.Request.Host.ToString() + base.HttpContext.Request.Path;
        string value2 = httpRequest.Path.Value;
        Uri uri = httpRequest.ToUri();
        string absoluteUri = uri.AbsoluteUri;
        string urlReferrerAbsolutePathOriginalPath = uri.PathAndQuery.ToString();
        try
        {
            dataPrivacyCollectorInterface = new DataPrivacyCollectorInterface(app_settings.dp_settings);
        }
        catch (Exception ex)
        {
            string text = ex.Message;
            if (ex.InnerException != null)
            {
                text = text + "<br>" + ex.InnerException.Message;
            }
            throw new Exception(text);
        }
        ApplicationLogging.LogInformation("Controller request {0}.{1} --> {2}", httpRequest.Method, httpRequest.Path, JsonConvert.SerializeObject(req, Formatting.Indented));
        startTime = DateTime.Now;
        if (req.ctx.user.DescrizioneUtente == null)
        {
            req.ctx.user.DescrizioneUtente = "Utente: " + req.ctx.user.CodiceUtente;
        }
        userinfo = "";
        if (req != null)
        {
            userinfo = req.ctx.user.CodiceAbiDefault.PadLeft(5, '0') + "|" + req.ctx.user.CodiceUtente.PadRight(6) + "|" + req.ctx.user.DescrizioneUtente.PadRight(40) + "|" + req.ctx.user.CodiceToken;
        }
        if (base.Request.Method.ToString() == "OPTIONS")
        {
            return dataPrivacyCollectorInterface;
        }
        bool validateToken = app_settings.Security.ValidateToken;
        string technicalUsers = app_settings.Security.TechnicalUsers;
        string codiceUtente = req.ctx.user.CodiceUtente;
        string codiceToken = req.ctx.user.CodiceToken;
        bool urlAccessAPL = true;
        dataPrivacyCollectorInterface = new DataPrivacyCollectorInterface(app_settings.dp_settings);
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.IsUsed = false;
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.TipoCodiceCliente = " ";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.CodiceCliente = FindCodiceCliente(req);
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.CodiceProcedura = "APP_" + req.ctx.application.CodApplicazione.ToString().PadLeft(3, '0');
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.MacroAmbito = "";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.Modulo = "";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.CodiceFunzione = "";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.DescrizioneFunzione = req.ctx.application.DescrizioneApplicazione;
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.MacroContenuto = "0";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.MolteplicitaPF = "0";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.MolteplicitaRapporti = "0";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.MolteplicitaMovim = "0";
        dataPrivacyCollectorInterface.dataPrivacyEntity.Config.SottoFunzioneInterna = "I";
        if (!string.IsNullOrEmpty(dataPrivacyCollectorInterface.dataPrivacyEntity.Context.CodiceCliente))
        {
            dataPrivacyCollectorInterface.dataPrivacyEntity.Context.CodiceCliente = dataPrivacyCollectorInterface.dataPrivacyEntity.Context.CodiceCliente.PadLeft(16, '0');
            dataPrivacyCollectorInterface.dataPrivacyEntity.Context.TipoCodiceCliente = "NAG";
            dataPrivacyCollectorInterface.dataPrivacyEntity.Config.IsUsed = true;
        }
        dataPrivacyCollectorInterface.GetDataPrivacy(httpRequest.HttpContext, req, value, absolutePathOriginalPath, absoluteUri, urlReferrerAbsolutePathOriginalPath, value2, urlAccessAPL);
        if (httpRequest.Headers.ContainsKey("Authorization"))
        {
            string text2 = httpRequest.Headers["Authorization"];
            if (text2.ToUpper().StartsWith("BASIC"))
            {
                text2 = text2.Substring("BASIC ".Length);
            }
            byte[] bytes = Convert.FromBase64String(text2);
            string[] array = Encoding.UTF8.GetString(bytes).Split(':');
            string headerUserid = string.Empty;
            string text3 = string.Empty;
            if (array.Length == 2)
            {
                headerUserid = array[0];
                text3 = array[1];
            }
            if (validateToken && !forceNoTokenValidation)
            {
                List<string> list = new List<string>();
                bool flag = false;
                new List<string>();
                if (string.IsNullOrEmpty(technicalUsers))
                {
                    flag = false;
                }
                else
                {
                    flag = true;
                    string[] array2 = technicalUsers.Split(';');
                    list = new List<string>();
                    string[] array3 = array2;
                    foreach (string item in array3)
                    {
                        list.Add(item);
                    }
                }
                bool flag2 = false;
                if (flag && list.Find((string x) => x.Trim().ToUpper() == headerUserid.Trim().ToUpper()) != null && !string.IsNullOrEmpty(list.Find((string x) => x.Trim().ToUpper() == headerUserid.Trim().ToUpper())))
                {
                    flag2 = true;
                }
                if (!flag2 && !app_settings.dp_settings.GenericSettings.AvoidHeaderCheck && (!(headerUserid.Trim().ToUpper() == codiceUtente.Trim().ToUpper()) || !(text3.Trim().ToUpper() == codiceToken.Trim().ToUpper())))
                {
                    throw new Exception("Missmatch tra userid/token di header " + headerUserid + "/" + text3 + " e userid/token di contesto o Utente tecnico non riconosciuto");
                }
                if (base.Request.Cookies.ContainsKey("TokenSessionId") && base.Request.Cookies["TokenSessionId"] != null && codiceToken.Trim() != base.Request.Cookies["TokenSessionId"].Trim())
                {
                    throw new Exception("La validazione del TokenSessionId di sicurezza è fallita");
                }
                if (!CSDValidateToken.ValidateToken(codiceUtente.Trim().ToUpper(), codiceToken))
                {
                    throw new Exception("Il token di sicurezza non è valido o è scaduto");
                }
                string text4 = req.ctx.application.CodApplicazione.ToString();
                if (text4.Trim() == "1702")
                {
                    text4 = "170";
                }
            }
        }
        else if (httpRequest.Headers != null && httpRequest.Headers.ContainsKey("MS_HttpContext"))
        {
            string text5 = httpRequest.Headers["MS_HttpContext"];
            string autoValidatedReverseProxyServers = app_settings.dp_settings.ReverseProxy.AutoValidatedReverseProxyServers;
            if (string.IsNullOrEmpty(autoValidatedReverseProxyServers) && !autoValidatedReverseProxyServers.Contains(text5))
            {
                throw new Exception("La chiamata proviene da un reverse proxy server non valido: " + text5);
            }
        }
        else if (validateToken && !forceNoTokenValidation)
        {
            throw new Exception("Il token di sicurezza non può essere validato se non presente nell' Authorization Header");
        }
        return dataPrivacyCollectorInterface;
    }

    protected void TerminateService(DataPrivacyCollectorInterface dataPrivacy, CSDRequest request, CSDResponse res, string stackTrace, bool isMultiple = false, Dictionary<string, string> multipleQueryParams = null)
    {
        if (dataPrivacy == null)
        {
            res.Rc = -1;
            res.RcDescription += "dataPrivacy == null;<br> ";
            res.RcInfo = "Error in TerminateService ";
            return;
        }
        bool mongoDB_LogOnlyErrors = app_settings.errlog_settings.MongoDB_LogOnlyErrors;
        if ((mongoDB_LogOnlyErrors && res.Rc != 0) || !mongoDB_LogOnlyErrors)
        {
            try
            {
                res.ErrorGuid = Guid.NewGuid().ToString();
                if (!string.IsNullOrEmpty(dataPrivacy.dataPrivacyEntity._id))
                {
                    res.ErrorGuid = dataPrivacy.dataPrivacyEntity._id;
                }
                new MongoDB_ErrLogger(app_settings.errlog_settings, request.ctx.application.Environment).AddError(dataPrivacy.dataPrivacyEntity, request, res, res.ErrorGuid, stackTrace);
            }
            catch (Exception exc)
            {
                ApplicationLogging.LogError("TerminateService failure {0}", exc);
            }
        }
        string description = base.Request.ToUri().Segments[1] + base.Request.ToUri().Segments[base.Request.ToUri().Segments.Length - 1];
        TimeSpan timeSpan = DateTime.Now - startTime;
        CSDTimer cSDTimer = new CSDTimer();
        cSDTimer.description = description;
        cSDTimer.elapsed = (long)timeSpan.TotalMilliseconds;
        res.RcTimers.Add(cSDTimer);
        try
        {
            if (isMultiple && dataPrivacy != null && dataPrivacy.dataPrivacyEntity != null)
            {
                dataPrivacy.dataPrivacyEntity.Config.MolteplicitaPF = "N";
                dataPrivacy.dataPrivacyEntity.Config.IsUsed = true;
                if (multipleQueryParams != null)
                {
                    dataPrivacy.dataPrivacyEntity.Params.Parameters = new Dictionary<string, string>();
                    dataPrivacy.dataPrivacyEntity.Params.Parameters = multipleQueryParams;
                }
            }
            if (dataPrivacy.dataPrivacyEntity != null)
            {
                dataPrivacy.ComputeElapsed();
                if (dataPrivacy != null && dataPrivacy.dataPrivacyEntity != null && dataPrivacy.dataPrivacyEntity.Request != null && dataPrivacy.dataPrivacyEntity.Request.ApplicationPath != null)
                {
                    dataPrivacy.DoLog(dataPrivacy.dataPrivacyEntity.Request.ApplicationPath);
                }
                else
                {
                    dataPrivacy.DoLog("");
                }
            }
        }
        catch (Exception exc2)
        {
            res = ApplicationLogging.LogErrorResponse("Error in TerminateService ", exc2);
        }
    }

    private string FindCodiceCliente(object req)
    {
        string text = "";
        try
        {
            Dictionary<string, object> dictionary = DictionaryFromType(req);
            foreach (string key in dictionary.Keys)
            {
                if (dictionary[key] != null && dictionary[key].GetType() != typeof(string) && dictionary[key].GetType() != typeof(int) && dictionary[key].GetType() != typeof(long) && dictionary[key].GetType() != typeof(double) && dictionary[key].GetType() != typeof(float) && dictionary[key].GetType() != typeof(DateTime))
                {
                    if (key.StartsWith("pars"))
                    {
                        if (dictionary[key] is CSDParam cSDParam && (cSDParam.pn.ToString().ToLower() == "ndg" || cSDParam.pn.ToString().ToLower() == "codndg" || cSDParam.pn.ToString().ToLower() == "codndg_0" || cSDParam.pn.ToString().ToLower() == "cod_ndg" || cSDParam.pn.ToString().ToLower() == "cod_nag_0" || cSDParam.pn.ToString().ToLower() == "cod-ndg" || cSDParam.pn.ToString().ToLower() == "nag" || cSDParam.pn.ToString().ToLower() == "codnag" || cSDParam.pn.ToString().ToLower() == "codnag_0" || cSDParam.pn.ToString().ToLower() == "cod_nag_0" || cSDParam.pn.ToString().ToLower() == "cod_nags_0" || cSDParam.pn.ToString().ToLower() == "cod-nag" || cSDParam.pn.ToString().ToLower() == "cod-cli" || cSDParam.pn.ToString().ToLower() == "cag" || cSDParam.pn.ToString().ToLower() == "cliente" || cSDParam.pn.ToString().ToLower() == "cod_cliente" || cSDParam.pn.ToString().ToLower() == "codice-cliente"))
                        {
                            return cSDParam.pv.ToString();
                        }
                        continue;
                    }
                    text = FindCodiceCliente(dictionary[key]);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
                else if (key.ToString().ToLower() == "ndg" || key.ToString().ToLower() == "codndg" || key.ToString().ToLower() == "codndg_0" || key.ToString().ToLower() == "cod_ndg" || key.ToString().ToLower() == "cod-ndg" || key.ToString().ToLower() == "cod_nag_0" || key.ToString().ToLower() == "nag" || key.ToString().ToLower() == "codnag" || key.ToString().ToLower() == "cod_nag" || key.ToString().ToLower() == "codnag_0" || key.ToString().ToLower() == "cod_nag_0" || key.ToString().ToLower() == "cod_nags_0" || key.ToString().ToLower() == "cod_cli" || key.ToString().ToLower() == "cag" || key.ToString().ToLower() == "cliente" || key.ToString().ToLower() == "cod_cliente" || key.ToString().ToLower() == "codice-cliente")
                {
                    return dictionary[key].ToString().PadLeft(16, '0');
                }
            }
        }
        catch (Exception)
        {
            text = string.Empty;
        }
        if (!string.IsNullOrEmpty(text))
        {
            return text.PadLeft(16, '0');
        }
        return text;
    }

    private static Dictionary<string, object> DictionaryFromType(object atype)
    {
        if (atype == null)
        {
            return new Dictionary<string, object>();
        }
        PropertyInfo[] properties = atype.GetType().GetProperties();
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        PropertyInfo[] array = properties;
        foreach (PropertyInfo propertyInfo in array)
        {
            if (propertyInfo.PropertyType.GetInterface(typeof(IEnumerable<>).FullName) != null)
            {
                if (propertyInfo.GetValue(atype, new object[0]) is IEnumerable<object> enumerable)
                {
                    int num = 0;
                    foreach (object item in enumerable)
                    {
                        dictionary.Add(propertyInfo.Name + "_" + num, item);
                        num++;
                    }
                }
                else
                {
                    object value = propertyInfo.GetValue(atype, new object[0]);
                    dictionary.Add(propertyInfo.Name, value);
                }
            }
            else
            {
                object value2 = propertyInfo.GetValue(atype, new object[0]);
                dictionary.Add(propertyInfo.Name, value2);
            }
        }
        return dictionary;
    }
}

#region Assembly CSD.Framework.NetCore, Version=1.0.74.3786, Culture=neutral, PublicKeyToken=null
// CSD.Framework.NetCore.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

public class ActionService
{
    public CSDContext Ctx { get; set; }

    public CSDDatabase Db { get; set; }

    public ActionService(CSDContext ctx)
    {
        Ctx = ctx;
    }

    public ActionService(CSDDatabase db, CSDContext ctx)
    {
        Ctx = ctx;
        Db = db;
    }

    public List<string> GetActionsForUser(string userName, string codAbi, string codApl)
    {
        List<string> list = new List<string>();
        string text = "SELECT DISTINCT CodiceStruttura\r\n                                FROM RIS_USR_APPARTENENZA ";
        text += $"\r\n                                WHERE\r\n                                CodiceABI = '{codAbi}' AND CodiceRIS = '{userName}'\r\n                                AND Tipologia = 'AZI' AND CodiceAPL='{codApl}'";
        if (Db == null)
        {
            Db = DataAccessApplicationBlock.SetDatabaseFactory(DBType.TBSICUREZZA, Ctx);
        }
        foreach (DataRow row in ((IDataProviderQueryGenerica)new CSDFrameworkPMSPatch.DataProviderQueryGenerica(Db, Ctx)).GetDatiQueryGenericaTBSicurezza(text, new Dictionary<string, object>()).Tables[0].Rows)
        {
            list.Add(row.ItemArray[0].ToString());
        }
        return list;
    }

    public List<string> GetFullActionsWorkFlowForUser(string userName, string codAbi, string codApl)
    {
        List<string> list = new List<string>();
        string text = "\r\n            SELECT  COUNT(*) \r\n            FROM    {TBBASE}RIS_USR_APPARTENENZA_{ABI4} ";
        text += $"\r\n            WHERE   CODICEABI =  '{codAbi}' \r\n                AND CODICERIS =  '{userName}' \r\n                AND TIPOLOGIA = 'AZI'   \r\n                AND CODICEAPL = '103'   \r\n                AND CODICEPURO = 'Modifica' ";
        if (Db == null)
        {
            Db = DataAccessApplicationBlock.SetDatabaseFactory(DBType.TBBASE, Ctx);
        }
        foreach (DataRow row in ((IDataProviderQueryGenerica)new DataProviderQueryGenerica(Db, Ctx)).GetDatiQueryGenericaTBBase(text, new Dictionary<string, object>(), "").Tables[0].Rows)
        {
            list.Add(row.ItemArray[0].ToString());
        }
        return list;
    }
}


#region Assembly CSD.Framework.NetCore, Version=1.0.74.3786, Culture=neutral, PublicKeyToken=null
// CSD.Framework.NetCore.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

public static class DataAccessApplicationBlock
{
    public static IConfiguration AppConfiguration { get; set; }

    public static IDataProviderNetCoreConfig dataProviderNetCore { get; set; }

    public static CSDDatabase SetDatabaseFactory(DBType databaseType, CSDContext ctx)
    {
        CSDDatabase cSDDatabase = new CSDDatabase();
        try
        {
            DataProviderNetCoreConfig dataProviderNetCoreConfig = new DataProviderNetCoreConfig();
            cSDDatabase.DbConfig = dataProviderNetCoreConfig.GetSqlConnConfig(databaseType, ctx);
            return cSDDatabase;
        }
        catch (Exception innerException)
        {
            AppEnvironment currentEnvironment = CtxEnvironment.GetCurrentEnvironment();
            throw new Exception("Errore in CreateDatabase connection " + databaseType.ToString() + ": Environment=" + currentEnvironment, innerException);
        }
    }

    public static CSDDatabase SetDatabaseFactory(DBType DbType, CSDContext ctx, string CodiceScenario)
    {
        try
        {
            return SetDatabaseFactory(DbType, ctx);
        }
        catch (Exception innerException)
        {
            AppEnvironment currentEnvironment = CtxEnvironment.GetCurrentEnvironment();
            throw new Exception("Errore in SetDatabaseFactory connection " + DbType.ToString() + ": Environment=" + currentEnvironment, innerException);
        }
    }

    public static CSDOraDatabase SetOraDatabaseFactory(DBType databaseType, CSDContext ctx)
    {
        CSDOraDatabase cSDOraDatabase = new CSDOraDatabase();
        try
        {
            DataProviderNetCoreConfig dataProviderNetCoreConfig = new DataProviderNetCoreConfig();
            cSDOraDatabase.DbConfig = dataProviderNetCoreConfig.GetSqlConnConfig(databaseType, ctx);
            return cSDOraDatabase;
        }
        catch (Exception innerException)
        {
            AppEnvironment currentEnvironment = CtxEnvironment.GetCurrentEnvironment();
            throw new Exception("Errore in CreateDatabase connection " + databaseType.ToString() + ": Environment=" + currentEnvironment, innerException);
        }
    }

    public static CSDOraDatabase SeOratDatabaseFactory(DBType DbType, CSDContext ctx, string CodiceScenario)
    {
        try
        {
            return SetOraDatabaseFactory(DbType, ctx);
        }
        catch (Exception innerException)
        {
            AppEnvironment currentEnvironment = CtxEnvironment.GetCurrentEnvironment();
            throw new Exception("Errore in SetOraDatabaseFactory connection " + DbType.ToString() + ": Environment=" + currentEnvironment, innerException);
        }
    }
}
#region Assembly CSD.Framework.NetCore, Version=1.0.74.3786, Culture=neutral, PublicKeyToken=null
// CSD.Framework.NetCore.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

public class DataProviderNetCoreConfig : IDataProviderNetCoreConfig
{
    private static string DEFAULT_APP_ENVIRONMENT = "DEVE";

    private static string DEFAULT_DBTYPE = DBType.TBBASE.ToString();

    private static string DEFAULT_HOLDING = "('','00CCB','03599')";

    private const string CreateConfigTables = "\r\n        IF OBJECT_ID('dbo.WEBAPP_DBCONF', 'U') IS NOT NULL \r\n  DROP TABLE dbo.WEBAPP_DBCONF; \r\n\r\nCREATE TABLE dbo.WEBAPP_DBCONF \r\n(\r\n\tAPPENV CHAR(8) NOT NULL,\r\n\tDBTYPE CHAR(8)  NOT NULL,\r\n\tCOD_HOLDING CHAR(5)  NOT NULL,\r\n\tCOD_ABI CHAR(5)  NOT NULL,\r\n\tCOD_APP INT,\r\n\tCONNECTION_STR VARCHAR(128) NOT NULL,\r\n\tCONNECTION_PREFIX VARCHAR(128),\r\n\tDB_USERID VARCHAR(24),\r\n\tDB_PASSWORD VARCHAR(64),\r\n\tCONSTRAINT PK_WEBAPP_DBCONF PRIMARY KEY (APPENV,DBTYPE,COD_HOLDING,COD_ABI,COD_APP)\r\n) \r\n\r\nIF OBJECT_ID('dbo.WEBAPP_APPCONF', 'U') IS NOT NULL \r\n  DROP TABLE dbo.WEBAPP_APPCONF; \r\n\r\nCREATE TABLE dbo.WEBAPP_APPCONF \r\n(\r\n\tAPPENV CHAR(8) NOT NULL,\r\n\tDBTYPE CHAR(8)  NOT NULL,\r\n\tCOD_HOLDING CHAR(5)  NOT NULL,\r\n\tCOD_ABI CHAR(5)  NOT NULL,\r\n\tCOD_APP INT,\r\n\tAPP_KEY\tVARCHAR(16),\r\n\tAPP_VALUE\tVARCHAR(256),\r\n\tCONSTRAINT PK_WEBAPP_APPCONF PRIMARY KEY (APPENV,DBTYPE,COD_HOLDING,COD_ABI,COD_APP)\r\n)\r\n\r\n";

    private const string GetConnectionString = "\r\n            SELECT \r\n                WEBAPP_DBCONF.APPENV AS APPENV, \r\n                WEBAPP_DBCONF.DBTYPE AS DBTYPE,\r\n                WEBAPP_DBCONF.COD_HOLDING AS COD_HOLDING, \r\n                WEBAPP_DBCONF.COD_ABI AS COD_ABI,\r\n                WEBAPP_DBCONF.COD_APP AS COD_APP,\r\n                WEBAPP_DBCONF.CONNECTION_STR AS CONNECTION_STR,\r\n                WEBAPP_DBCONF.CONNECTION_PREFIX AS CONNECTION_PREFIX,\r\n                WEBAPP_DBCONF.DB_USERID AS DB_USERID,\r\n                WEBAPP_DBCONF.DB_PASSWORD AS DB_PASSWORD, \r\n                SWA.SERVER_FISICO AS LINKED_SRV \r\n            FROM WEBAPP_DBCONF as WEBAPP_DBCONF \r\n            LEFT OUTER JOIN SID_WEB_ABI as SWA ON \r\n                WEBAPP_DBCONF.COD_ABI = SWA.COD_ABI\r\n            WHERE \r\n                (WEBAPP_DBCONF.APPENV = @app_env OR WEBAPP_DBCONF.APPENV = '{1}') AND\r\n                (WEBAPP_DBCONF.DBTYPE = @db_type OR WEBAPP_DBCONF.DBTYPE = '{2}') AND\r\n                (WEBAPP_DBCONF.COD_HOLDING = @cod_holding OR WEBAPP_DBCONF.COD_HOLDING IN {3}) AND\r\n                (WEBAPP_DBCONF.COD_ABI = @cod_abi) AND            \r\n                (WEBAPP_DBCONF.COD_APP = -1 OR WEBAPP_DBCONF.COD_APP = @cod_app)\r\n            ";

    private static IConfiguration Configuration { get; set; }

    private CSDContext Ctx { get; set; }

    private CSDDatabase Db { get; set; }

    public string EncryptInfo(string info)
    {
        string text = "";
        if (!string.IsNullOrEmpty(info))
        {
            text = RijndaelSimple.DefaultEncrypt(info);
        }
        if (string.IsNullOrEmpty(text))
        {
            text = info;
        }
        return text;
    }

    public string DecryptInfo(string encritptedInfo)
    {
        string text = "";
        if (!string.IsNullOrEmpty(encritptedInfo))
        {
            text = RijndaelSimple.DefaultDecrypt(encritptedInfo);
        }
        if (string.IsNullOrEmpty(text))
        {
            text = encritptedInfo;
        }
        return text;
    }

    public SqlConnection GetSqlConnection(DBType dbtype, CSDContext ctx)
    {
        EntityDatabaseConfig sqlConnConfig = new DataProviderNetCoreConfig().GetSqlConnConfig(dbtype, ctx);
        if (sqlConnConfig == null)
        {
            ApplicationLogging.LogError("SQL Configuration not found in table WEBAPP_DBCONF with parameters :  DBTYPE={1} HOLDING={2} ABI={3} APPID={4} ", dbtype.ToString(), ctx.user.CodiceHolding, ctx.user.CodiceAbiDefault, ctx.application.CodApplicazione.ToString());
            throw new Exception($"SQL DataBase Conection String Found for :  DBTYPE={dbtype.ToString()} HOLDING={ctx.user.CodiceHolding} ABI={ctx.user.CodiceAbiDefault} APPID={ctx.application.CodApplicazione.ToString()}");
        }
        return new SqlConnection(sqlConnConfig.ConnectionString);
    }

    public EntityDatabaseConfig GetSqlConnConfig(DBType dbtype, CSDContext ctx)
    {
        EntityDatabaseConfig entityDatabaseConfig = null;
        Configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
        AppEnvironment env = CtxEnvironment.GetCurrentEnvironment();
        entityDatabaseConfig = ConfigurationCacheStorage.GetConnetion(env, dbtype, ctx.user.CodiceHolding, ctx.user.CodiceAbiDefault, ctx.application.CodApplicazione);
        if (entityDatabaseConfig != null)
        {
            return entityDatabaseConfig;
        }
        CSDDatabase cSDDatabase = new CSDDatabase();
        try
        {
            cSDDatabase.DbConfig = new EntityDatabaseConfig();
            cSDDatabase.DbConfig.AppEnv = env;
            cSDDatabase.DbConfig.DbType = DBType.TBCONFIG;
            cSDDatabase.DbConfig.ConnectionString = Configuration.GetConnectionString("TBCONFIG");
            List<SqlParameter> list = new List<SqlParameter>();
            list.Add(new SqlParameter
            {
                ParameterName = "@app_env",
                SqlDbType = SqlDbType.VarChar,
                Value = env.ToString()
            });
            list.Add(new SqlParameter
            {
                ParameterName = "@db_type",
                SqlDbType = SqlDbType.VarChar,
                Value = dbtype.ToString()
            });
            list.Add(new SqlParameter
            {
                ParameterName = "@cod_holding",
                SqlDbType = SqlDbType.VarChar,
                Value = ctx.user.CodiceHolding
            });
            list.Add(new SqlParameter
            {
                ParameterName = "@cod_abi",
                SqlDbType = SqlDbType.VarChar,
                Value = ctx.user.CodiceAbiDefault
            });
            list.Add(new SqlParameter
            {
                ParameterName = "@cod_app",
                SqlDbType = SqlDbType.Int,
                Value = ctx.application.CodApplicazione
            });
            string sqlSrc = string.Format("\r\n            SELECT \r\n                WEBAPP_DBCONF.APPENV AS APPENV, \r\n                WEBAPP_DBCONF.DBTYPE AS DBTYPE,\r\n                WEBAPP_DBCONF.COD_HOLDING AS COD_HOLDING, \r\n                WEBAPP_DBCONF.COD_ABI AS COD_ABI,\r\n                WEBAPP_DBCONF.COD_APP AS COD_APP,\r\n                WEBAPP_DBCONF.CONNECTION_STR AS CONNECTION_STR,\r\n                WEBAPP_DBCONF.CONNECTION_PREFIX AS CONNECTION_PREFIX,\r\n                WEBAPP_DBCONF.DB_USERID AS DB_USERID,\r\n                WEBAPP_DBCONF.DB_PASSWORD AS DB_PASSWORD, \r\n                SWA.SERVER_FISICO AS LINKED_SRV \r\n            FROM WEBAPP_DBCONF as WEBAPP_DBCONF \r\n            LEFT OUTER JOIN SID_WEB_ABI as SWA ON \r\n                WEBAPP_DBCONF.COD_ABI = SWA.COD_ABI\r\n            WHERE \r\n                (WEBAPP_DBCONF.APPENV = @app_env OR WEBAPP_DBCONF.APPENV = '{1}') AND\r\n                (WEBAPP_DBCONF.DBTYPE = @db_type OR WEBAPP_DBCONF.DBTYPE = '{2}') AND\r\n                (WEBAPP_DBCONF.COD_HOLDING = @cod_holding OR WEBAPP_DBCONF.COD_HOLDING IN {3}) AND\r\n                (WEBAPP_DBCONF.COD_ABI = @cod_abi) AND            \r\n                (WEBAPP_DBCONF.COD_APP = -1 OR WEBAPP_DBCONF.COD_APP = @cod_app)\r\n            ", "TBCONFIG.dbo.", DEFAULT_APP_ENVIRONMENT, DEFAULT_DBTYPE, DEFAULT_HOLDING);
            SqlDataReader sqlDataReader = cSDDatabase.ExecuteReader(sqlSrc, list);
            List<EntityDatabaseConfig> list2 = new List<EntityDatabaseConfig>();
            if (sqlDataReader.HasRows)
            {
                while (sqlDataReader.Read())
                {
                    EntityDatabaseConfig entityDatabaseConfig2 = new EntityDatabaseConfig();
                    try
                    {
                        entityDatabaseConfig2.AppEnv = (AppEnvironment)Enum.Parse(typeof(AppEnvironment), sqlDataReader.GetString(sqlDataReader.GetOrdinal("APPENV")));
                        entityDatabaseConfig2.DbType = (DBType)Enum.Parse(typeof(DBType), sqlDataReader.GetString(sqlDataReader.GetOrdinal("DBTYPE")));
                        entityDatabaseConfig2.Holding = sqlDataReader.GetString(sqlDataReader.GetOrdinal("COD_HOLDING")).Trim();
                        entityDatabaseConfig2.CodAbi = sqlDataReader.GetString(sqlDataReader.GetOrdinal("COD_ABI")).Trim();
                        entityDatabaseConfig2.CodApp = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("COD_APP"));
                        entityDatabaseConfig2.DbUserid = sqlDataReader.GetString(sqlDataReader.GetOrdinal("DB_USERID")).Trim();
                        entityDatabaseConfig2.DbPassword = sqlDataReader.GetString(sqlDataReader.GetOrdinal("DB_PASSWORD")).Trim();
                        entityDatabaseConfig2.Prefix = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CONNECTION_PREFIX")).Trim();
                        entityDatabaseConfig2.ConnectionString = sqlDataReader.GetString(sqlDataReader.GetOrdinal("CONNECTION_STR")).Trim();
                        entityDatabaseConfig2.LinkedServer = ((!sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("LINKED_SRV"))) ? sqlDataReader.GetString(sqlDataReader.GetOrdinal("LINKED_SRV")).Trim() : "");
                        if (!string.IsNullOrEmpty(entityDatabaseConfig2.DbPassword))
                        {
                            entityDatabaseConfig2.DbPassword = DecryptInfo(entityDatabaseConfig2.DbPassword);
                        }
                        if (!string.IsNullOrEmpty(entityDatabaseConfig2.DbUserid) && !string.IsNullOrEmpty(entityDatabaseConfig2.DbPassword))
                        {
                            entityDatabaseConfig2.ConnectionString = string.Format(entityDatabaseConfig2.ConnectionString, entityDatabaseConfig2.DbUserid, entityDatabaseConfig2.DbPassword);
                        }
                        list2.Add(entityDatabaseConfig2);
                    }
                    catch (Exception exc)
                    {
                        ApplicationLogging.LogError("Exception in configuration search (WEBAPP_DBCONF) getting entities \r\n  {0} ", exc);
                    }
                }
                if (!sqlDataReader.IsClosed)
                {
                    sqlDataReader.Close();
                    sqlDataReader.Dispose();
                }
            }
            if (list2.Count == 0)
            {
                throw new Exception($"SQL Configuration missing in table WEBAPP_DBCONF con parameteri : APPENV={env.ToString()} DBTYPE={dbtype.ToString()} HOLDING={ctx.user.CodiceHolding} ABI={ctx.user.CodiceAbiDefault} APPID={ctx.application.CodApplicazione.ToString()}");
            }
            entityDatabaseConfig = list2.Find((EntityDatabaseConfig x) => x.AppEnv.ToString().Trim().ToUpper() == env.ToString().Trim().ToUpper() && x.DbType.ToString().Trim().ToUpper() == dbtype.ToString().Trim().ToUpper() && x.Holding.ToString().Trim().ToUpper() == ctx.user.CodiceHolding.ToString().Trim().ToUpper() && x.CodAbi.ToString().Trim().ToUpper() == ctx.user.CodiceAbiDefault.ToString().Trim().ToUpper() && x.CodApp == ctx.application.CodApplicazione);
            if (entityDatabaseConfig != null)
            {
                ConfigurationCacheStorage.AddConnetion(entityDatabaseConfig);
                return entityDatabaseConfig;
            }
            entityDatabaseConfig = list2.Find((EntityDatabaseConfig x) => x.AppEnv.ToString().Trim().ToUpper() == env.ToString().Trim().ToUpper() && x.DbType.ToString().Trim().ToUpper() == dbtype.ToString().Trim().ToUpper() && x.Holding.ToString().Trim().ToUpper() == ctx.user.CodiceHolding.ToString().Trim().ToUpper() && x.CodAbi.ToString().Trim().ToUpper() == ctx.user.CodiceAbiDefault.ToString().Trim().ToUpper());
            if (entityDatabaseConfig != null)
            {
                ConfigurationCacheStorage.AddConnetion(entityDatabaseConfig);
                return entityDatabaseConfig;
            }
            entityDatabaseConfig = list2.Find((EntityDatabaseConfig x) => x.AppEnv.ToString().Trim().ToUpper() == env.ToString().Trim().ToUpper() && x.DbType.ToString().Trim().ToUpper() == dbtype.ToString().Trim().ToUpper() && x.CodAbi.ToString().Trim().ToUpper() == ctx.user.CodiceAbiDefault.ToString().Trim().ToUpper());
            if (entityDatabaseConfig != null)
            {
                ConfigurationCacheStorage.AddConnetion(entityDatabaseConfig);
                return entityDatabaseConfig;
            }
            entityDatabaseConfig = list2.Find((EntityDatabaseConfig x) => x.DbType.ToString().Trim().ToUpper() == env.ToString().Trim().ToUpper() && x.CodAbi.ToString().Trim().ToUpper() == ctx.user.CodiceAbiDefault.ToString().Trim().ToUpper());
            if (entityDatabaseConfig != null)
            {
                ConfigurationCacheStorage.AddConnetion(entityDatabaseConfig);
                return entityDatabaseConfig;
            }
            if (entityDatabaseConfig == null)
            {
                ApplicationLogging.LogError("SQL Configuration not found in table WEBAPP_DBCONF with parameters : APPENV={0} DBTYPE={1} HOLDING={2} ABI={3} APPID={4} ", env.ToString(), dbtype.ToString(), ctx.user.CodiceHolding, ctx.user.CodiceAbiDefault, ctx.application.CodApplicazione.ToString());
                throw new Exception($"SQL Configuration not found in table WEBAPP_DBCONF with parameters : APPENV={env.ToString()} DBTYPE={dbtype.ToString()} HOLDING={ctx.user.CodiceHolding} ABI={ctx.user.CodiceAbiDefault} APPID={ctx.application.CodApplicazione.ToString()}");
            }
            return entityDatabaseConfig;
        }
        catch (Exception ex)
        {
            ApplicationLogging.LogError("Exception in configuration search (WEBAPP_DBCONF) {0} ", ex);
            throw new Exception("Exception in configuration search (WEBAPP_DBCONF) {0} ", ex);
        }
        finally
        {
            cSDDatabase.Close();
        }
    }
}

#region Assembly CSD.Framework.NetCore, Version=1.0.74.3786, Culture=neutral, PublicKeyToken=null
// CSD.Framework.NetCore.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

public class DataProviderQueryGenerica : IDataProviderQueryGenerica
{
    public CSDContext Ctx { get; set; }

    public CSDDatabase Db { get; set; }

    public DataProviderQueryGenerica(CSDContext ctx)
    {
        Ctx = ctx;
    }

    public DataProviderQueryGenerica(CSDDatabase db, CSDContext ctx)
    {
        Ctx = ctx;
        Db = db;
    }

    private DataSet GetDatiQueryGenerica(CSDDatabase Db, string NomeMetodo, string Query, Dictionary<string, object> Parametri, string Scenario = "")
    {
        DataSet dataSet = null;
        string arg = Query;
        try
        {
            SqlCommand sqlCommand = GetSqlCommand(Query, Parametri, Scenario);
            arg = sqlCommand.CommandText;
            try
            {
                if (Db == null)
                {
                    Db = DataAccessApplicationBlock.SetDatabaseFactory(Db.DbConfig.DbType, Ctx);
                }
                ServerClassesUtility.DebugTsqlCommand(sqlCommand);
                if (sqlCommand.Connection == null)
                {
                    sqlCommand.Connection = Db.GetConnection();
                }
                return Db.ExecuteDataSet(sqlCommand);
            }
            finally
            {
                Db.Close();
            }
        }
        catch (Exception ex)
        {
            throw new DataOperationException($"Errore durante la lettura ({NomeMetodo}):\nquery: {arg}\nerrore: {ex.Message}", ex);
        }
    }

    private object GetValoreQueryGenerica(CSDDatabase db, string NomeMetodo, string Query, Dictionary<string, object> Parametri, string Scenario = "")
    {
        object obj = null;
        string arg = Query;
        try
        {
            SqlCommand sqlCommand = GetSqlCommand(Query, Parametri, Scenario);
            arg = sqlCommand.CommandText;
            try
            {
                if (Db == null)
                {
                    Db = db;
                }
                ServerClassesUtility.DebugTsqlCommand(sqlCommand);
                return Db.ExecuteScalar(sqlCommand);
            }
            finally
            {
                Db.Close();
            }
        }
        catch (Exception ex)
        {
            throw new DataOperationException($"Errore durante la lettura ({NomeMetodo}):\nquery: {arg}\nerrore: {ex.Message}", ex);
        }
    }

    private SqlCommand GetSqlCommand(string Query, Dictionary<string, object> Parametri, string Scenario = "")
    {
        string text = Query;
        SqlCommand sqlCommand = new SqlCommand();
        CSDDatabase cSDDatabase = DataAccessApplicationBlock.SetDatabaseFactory(DBType.TBBASE, Ctx);
        CSDDatabase cSDDatabase2 = DataAccessApplicationBlock.SetDatabaseFactory(DBType.TBCONFIG, Ctx);
        CSDDatabase cSDDatabase3 = DataAccessApplicationBlock.SetDatabaseFactory(DBType.TBSICUREZZA, Ctx);
        if (!string.IsNullOrEmpty(Ctx.user.CodiceAbiDefault))
        {
            text = text.Replace("{ABI}", Ctx.user.CodiceAbiDefault);
            text = text.Replace("{ABI4}", Ctx.user.CodiceAbiDefault.Substring(1));
        }
        text = text.Replace("{TBCONFIG}", cSDDatabase2.DbConfig.Prefix);
        text = text.Replace("{TBSICUREZZA}", cSDDatabase3.DbConfig.Prefix);
        text = ((!string.IsNullOrWhiteSpace(Scenario)) ? text.Replace("{TBBASE}", cSDDatabase.DbConfig.Prefix) : text.Replace("{TBBASE}", cSDDatabase.DbConfig.Prefix));
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandText = text;
        if (Parametri != null)
        {
            foreach (string key in Parametri.Keys)
            {
                if (Parametri[key].GetType() == typeof(string))
                {
                    sqlCommand.Parameters.Add("@" + key, SqlDbType.Char);
                }
                if (Parametri[key].GetType() == typeof(int))
                {
                    sqlCommand.Parameters.Add("@" + key, SqlDbType.Int);
                }
                if (Parametri[key].GetType() == typeof(long))
                {
                    sqlCommand.Parameters.Add("@" + key, SqlDbType.BigInt);
                }
                if (Parametri[key].GetType() == typeof(double))
                {
                    sqlCommand.Parameters.Add("@" + key, SqlDbType.Float);
                }
                sqlCommand.Parameters["@" + key].Value = Parametri[key];
            }
        }
        return sqlCommand;
    }

    public DataSet GetDatiQueryGenericaTBBase(string Query, Dictionary<string, object> Parametri, string Scenario = "")
    {
        new DataSet();
        return GetDatiQueryGenerica(Db, "GetDatiQueryGenericaTBBase", Query, Parametri, Scenario);
    }

    public DataSet GetDatiQueryGenericaTBConfig(string Query, Dictionary<string, object> Parametri)
    {
        new DataSet();
        return GetDatiQueryGenerica(Db, "GetDatiQueryGenericaTBConfig", Query, Parametri);
    }

    public DataSet GetDatiQueryGenericaTBSicurezza(string Query, Dictionary<string, object> Parametri)
    {
        new DataSet();
        return GetDatiQueryGenerica(Db, "GetDatiQueryGenericaTBSicurezza", Query, Parametri);
    }

    public object GetValoreQueryGenericaTBBase(string Query, Dictionary<string, object> Parametri, string Scenario = "")
    {
        return GetValoreQueryGenerica(Db, "GetDatiQueryGenericaTBBase", Query, Parametri, Scenario);
    }

    public object GetValoreQueryGenericaTBConfig(string Query, Dictionary<string, object> Parametri)
    {
        return GetValoreQueryGenerica(Db, "GetDatiQueryGenericaTBConfig", Query, Parametri);
    }

    public object GetValoreQueryGenericaTBSicurezza(string Query, Dictionary<string, object> Parametri)
    {
        return GetValoreQueryGenerica(Db, "GetValoreQueryGenericaTBSicurezza", Query, Parametri);
    }
}
