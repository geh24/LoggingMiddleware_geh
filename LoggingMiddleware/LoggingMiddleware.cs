using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Common;
using NLog.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public static void init(string nlogConfig)
        {
            // Define variables and initialize NLog
            string domainName = AppDomain.CurrentDomain.FriendlyName;
            string logdir = AppContext.BaseDirectory + "/../data";
            GlobalDiagnosticsContext.Set("domainName", domainName);
            GlobalDiagnosticsContext.Set("logdir", logdir);
            InternalLogger.LogFile = logdir + "/" + domainName + "_internal.log";

            logFactory = NLogBuilder.ConfigureNLog(nlogConfig);
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

        private ConcurrentDictionary<string, long> cdictStartTicks = new ConcurrentDictionary<string, long>();

        public async Task InvokeAsync(HttpContext httpContext)
        {
            log.Info(HttpLogger.REQUEST, httpContext);
            cdictStartTicks.TryAdd(httpContext.TraceIdentifier, DateTime.Now.Ticks);

            await this._next(httpContext);

            long ticks1;
            long millis = -1;
            if (cdictStartTicks.TryRemove(httpContext.TraceIdentifier, out ticks1)) {
                //Ticks are in 100 ns
                millis = (DateTime.Now.Ticks - ticks1) / 10000;
            }

            log.Info(HttpLogger.RESPONSE, httpContext,
                httpContext.Response.ContentLength?.ToString(),
                (millis > 0) ? millis.ToString() : "",
                httpContext.Response.ContentType);

            if ( cdictStartTicks.Count > 100 ) {
                log.Debug("cdictStartTicks.Count: " + cdictStartTicks.Count);
                long test;
                long ticks2 = DateTime.Now.Ticks + 3000000000; // added 300 ms
                KeyValuePair<string, long>[] keyPairs = cdictStartTicks.ToArray();
                foreach (KeyValuePair<string, long> kp in keyPairs) {
                    if (kp.Value > ticks2) {
                        cdictStartTicks.TryRemove(kp.Key, out test);
                    }
                }
            }
        }
    }
}
