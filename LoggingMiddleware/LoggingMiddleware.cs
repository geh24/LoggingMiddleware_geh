using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Common;
using NLog.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private static bool bDebugBody = false;

        private readonly RequestDelegate _next;

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

        public static Logger getLoger()
        {
            return logFactory.GetCurrentClassLogger();
        }

        public static void enableDebugBody(bool b)
        {
            bDebugBody = b;
        }

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private ConcurrentDictionary<string, long> cdictStartTicks = new ConcurrentDictionary<string, long>();

        public async Task InvokeAsync(HttpContext httpContext)
        {
            log.Info(HttpLogger.REQUEST, httpContext);
            if ( bDebugBody )
            {
                string body;
                var req = httpContext.Request;

                // Allows using several time the stream in ASP.Net Core
                req.EnableBuffering();

                using (StreamReader reader
                    = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();
                }

                // Rewind, so the core is not lost when it looks the body for the request
                req.Body.Position = 0;
                log.Debug(HttpLogger.REQUEST, httpContext, body);
            }
            cdictStartTicks.TryAdd(httpContext.TraceIdentifier, DateTime.Now.Ticks);

            string responseBody = null;
            try
            {
                if (bDebugBody)
                {
                    using (var swapStream = new MemoryStream())
                    {
                        var originalResponseBody = httpContext.Response.Body;
                        httpContext.Response.Body = swapStream;

                        await this._next(httpContext);

                        swapStream.Seek(0, SeekOrigin.Begin);
                        responseBody = new StreamReader(swapStream).ReadToEnd();
                        swapStream.Seek(0, SeekOrigin.Begin);
                        await swapStream.CopyToAsync(originalResponseBody);
                        httpContext.Response.Body = originalResponseBody;
                    }
                }
                else
                {
                    await this._next(httpContext);
                }
            } catch(Exception e) {
                log.Error(HttpLogger.RESPONSE,
                    httpContext,
                    null,
                    null,
                    e.Message);
            }

            long millis = -1;
            if (cdictStartTicks.TryRemove(httpContext.TraceIdentifier, out long ticks1)) {
                //Ticks are in 100 ns
                millis = (DateTime.Now.Ticks - ticks1) / 10000;
            }

            long? len = httpContext.Response.ContentLength;
            if ( len == null && responseBody != null) {
                len = responseBody.Length;
            }

            log.Info(HttpLogger.RESPONSE,
                httpContext,
                len?.ToString(),
                (millis > 0) ? millis.ToString() : "",
                httpContext.Response.ContentType);

            if ( bDebugBody )
            {
                log.Debug(HttpLogger.RESPONSE,
                    httpContext,
                    responseBody.Length.ToString(),
                    (millis > 0) ? millis.ToString() : "",
                    httpContext.Response.ContentType
                    + "|" + responseBody);
            }

            // House-keeping of lost Responsemessages
            if ( cdictStartTicks.Count > 100 ) {
                log.Info("cdictStartTicks.Count: " + cdictStartTicks.Count);
                long test;
                long ticks2 = DateTime.Now.Ticks + 3000000000; // added 300 ms
                KeyValuePair<string, long>[] keyPairs = cdictStartTicks.ToArray();
                foreach (KeyValuePair<string, long> kp in keyPairs) {
                    if (kp.Value > ticks2) {
                        log.Warn("Remove Tick: " + kp.Key);
                        cdictStartTicks.TryRemove(kp.Key, out test);
                    }
                }
            }
        }
    }
}
