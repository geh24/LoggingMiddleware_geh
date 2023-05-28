using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Common;
using NLog.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggingMiddleware_geh
{
    public static class LoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoggingMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware_geh>();
        }
    }

    public class LoggingMiddleware_geh
    {
        private static LogFactory logFactory;
        private static HttpLogger log = HttpLogger.getLogger(nameof(LoggingMiddleware_geh));
        private static bool bTraceBody = false;
        private static bool bTraceHeaders = false;
        private static bool bTraceQueryString = false;

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

        public static LMLogger getLoger()
        {
            return (LMLogger)logFactory.GetCurrentClassLogger();
        }

        public static void enableTraceBody(bool b)
        {
            bTraceBody = b;
        }
        public static void enableTraceHeaders(bool b)
        {
            bTraceHeaders = b;
        }
        public static void enableTraceQueryString(bool b)
        {
            bTraceQueryString = b;
        }

        public LoggingMiddleware_geh(RequestDelegate next)
        {
            _next = next;
        }

        private ConcurrentDictionary<string, long> cdictStartTicks = new ConcurrentDictionary<string, long>();

        public async Task InvokeAsync(HttpContext httpContext)
        {
            log.Info(HttpLogger.REQUEST, httpContext);

            string headers = null;
            if (bTraceHeaders) {
                headers = httpContext.Request.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ":" + b);
            }
            string queryString = null;
            if (bTraceQueryString) {
                queryString = httpContext.Request.QueryString.ToString();
            }
            string body = null;
            if ( bTraceBody )
            {
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
            }
            if ( bTraceBody || bTraceHeaders || bTraceQueryString) {
                log.Trace(HttpLogger.REQUEST, httpContext, body, headers, queryString);
            }
            cdictStartTicks.TryAdd(httpContext.TraceIdentifier, DateTime.Now.Ticks);

            //replace MeoryStream by RecyclableMemoryStream
            //see https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream
            //see https://elanderson.net/2019/12/log-requests-and-responses-in-asp-net-core-3/ LogResponse
            string responseBody = null;
            string responseHeaders = null;
            try {
                if (bTraceBody)
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
                    if (bTraceHeaders) {
                        responseHeaders = httpContext.Response.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ":" + b);
                    }
                } else
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

            if ( bTraceBody || bTraceHeaders )
            {
                log.Trace(HttpLogger.RESPONSE,
                    httpContext,
                    responseBody?.Length.ToString(),
                    (millis > 0) ? millis.ToString() : "",
                    httpContext.Response.ContentType
                    + "|" + responseBody + "|" + responseHeaders);
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
