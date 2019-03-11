using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Common;
using NLog.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LoggingMiddleware
{
    public static class LoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoggingMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware>();
        }
    }

    public class LoggingMiddleware
    {
        private static LogFactory logFactory;
        private static HttpLogger log = HttpLogger.getLogger(nameof(LoggingMiddleware));
        private readonly RequestDelegate _next;

        private static int logOption = LOG_DEFAULT;
        public const int LOG_DEFAULT = 0;
        public const int LOG_FULL_RESPONSE = 0b00000001;

        public static void init()
        {
            // Define variables and initialize NLog
            string domainName = AppDomain.CurrentDomain.FriendlyName;
            string logdir = AppContext.BaseDirectory + "/../data";
            GlobalDiagnosticsContext.Set("domainName", domainName);
            GlobalDiagnosticsContext.Set("logdir", logdir);
            InternalLogger.LogFile = logdir + "/" + domainName + "_internal.log";

            logFactory = NLogBuilder.ConfigureNLog("nlog.config");
        }

        public static void setLogOption(int aLogOption)
        {
            logOption = aLogOption;
        }

        public static Logger getLoger()
        {
            return logFactory.GetCurrentClassLogger();
        } 

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            log.Info(HttpLogger.REQUEST, httpContext);

            await this._next(httpContext);

            var response = httpContext.Response;
            log.Info(HttpLogger.RESPONSE, httpContext,
                    response.ContentLength?.ToString(),
                    response.ContentType);
        }
    }
}
