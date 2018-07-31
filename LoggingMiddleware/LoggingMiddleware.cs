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

        //see https://exceptionnotfound.net/using-middleware-to-log-requests-and-responses-in-asp-net-core/?utm_campaign=Revue%20newsletter&utm_medium=Newsletter&utm_source=ASP.NET%20Weekly
        public async Task InvokeAsync(HttpContext httpContext)
        {
            log.Info(HttpLogger.REQUEST, httpContext);

            var originalBodyStream = httpContext.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                httpContext.Response.Body = responseBody;

                try {
                    await this._next(httpContext);
                } catch (Exception ex)
                {
                    log.Error(HttpLogger.REQUEST, httpContext);
                    log.Error(ex.Message);
                    throw;
                }

                bool bFullResponse = Convert.ToBoolean( logOption & LOG_FULL_RESPONSE );

                string response = await FormatResponse(httpContext.Response);
                log.Info(HttpLogger.RESPONSE,
                    httpContext,
                    response.Length.ToString(), bFullResponse ? response : "");

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return $"{text}";
        }

    }
}
