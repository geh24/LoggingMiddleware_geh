using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Security.Claims;

namespace LoggingMiddleware
{
    public class HttpLogger
    {
        public static HttpLogger getLogger(string name) {
            return new HttpLogger(name);
        }

        public const int REQUEST = 1;
        public const int RESPONSE = 2;

        private Logger log;

        private HttpLogger(string name) {
            log = LogManager.GetLogger(name);
        }

        public void Fatal(string msg, Exception ex = null)
        {
            log.Fatal(ex, msg);
        }
        public void Error(string msg, Exception ex = null)
        {
            log.Error(ex, msg);
        }
        public void Warn(string msg, Exception ex = null)
        {
            log.Warn(ex, msg);
        }
        public void Info(string msg, Exception ex = null)
        {
            log.Info(ex, msg);
        }
        public void Debug(string msg, Exception ex = null)
        {
            log.Debug(ex, msg);
        }
        public void Trace(string msg, Exception ex = null)
        {
            log.Trace(ex, msg);
        }

        private string formatLog(int type, HttpContext ctx, string msg1, string msg2) {
            string msg;
            switch (type) {
                case REQUEST:
                    ClaimsIdentity ci = (ClaimsIdentity)ctx.User.Identity;
                    msg = ctx.Connection.RemoteIpAddress + "|"
                              + ctx.Request.Host + "|" + ctx.Request.Method + "|"
                              + ctx.TraceIdentifier + "|"
                              + ctx.Request.Path + "|"
                              + "Request|" + ci.FindFirst("Serial")?.Value + "|"
                              + msg1 + "|" + msg2;
                    break;
                case RESPONSE:
                    if (ctx.Response.StatusCode == 403)
                    {
                        JObject rc = new JObject();
                        ClaimsIdentity ci2 = (ClaimsIdentity)ctx.User.Identity;
                        foreach ( Claim c in ci2.Claims )
                        {
                            rc.Add(c.Type, c.Value);
                        }
                        msg1 = rc.ToString();
                    }
                    msg = ctx.Connection.RemoteIpAddress + "|"
                              + ctx.Request.Host + "|" + ctx.Request.Method + "|"
                              + ctx.TraceIdentifier + "|"
                              + ctx.Request.Path + "|"
                              + "Response|" + ctx.Response.StatusCode + "|"
                              + msg1 + "|" + msg2;
                    break;
                default:
                    msg = ctx.Connection.RemoteIpAddress + "|Unknown";
                    break;
            }
            return msg;
        }

        public void Fatal(int type, HttpContext ctx, string msg1 = null, string msg2 = null)
        {
            log.Fatal(formatLog(type, ctx, msg1, msg2));
        }
        public void Error(int type, HttpContext ctx, string msg1 = null, string msg2 = null)
        {
            log.Error(formatLog(type, ctx, msg1, msg2));
        }
        public void Warn(int type, HttpContext ctx, string msg1 = null, string msg2 = null)
        {
            log.Warn(formatLog(type, ctx, msg1, msg2));
        }
        public void Info(int type, HttpContext ctx, string msg1 = null, string msg2 = null)
        {
            log.Info(formatLog(type, ctx, msg1, msg2));
        }
        public void Debug(int type, HttpContext ctx, string msg1 = null, string msg2 = null)
        {
            log.Debug(formatLog(type, ctx, msg1, msg2));
        }
        public void Trace(int type, HttpContext ctx, string msg1 = null, string msg2 = null, string msg3 = null)
        {
            log.Trace(formatLog(type, ctx, msg1, msg2));
        }

    }
}
