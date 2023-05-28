using NLog;
using System;
using System.Runtime.CompilerServices;

namespace LoggingMiddleware_geh
{
    public class LMLogger : Logger
    {
        private Logger log;
        private string name;

        public LMLogger(string name)
        {
            this.name = name;
            log = LogManager.GetLogger(name);
        }

        public static LMLogger getLogger(string name)
        {
            return new LMLogger(name);
        }

        public void Fatal(string msg, Exception ex = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Fatal, name, msg);
            theEvent.Properties["linenumber"] = "line " + lineNumber;
            theEvent.Exception = ex;
            log.Log(theEvent);
        }
        public void Error(string msg, Exception ex = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Error, name, msg);
            theEvent.Properties["linenumber"] = "line " + lineNumber;
            theEvent.Exception = ex;
            log.Log(theEvent);
        }
        public void Warn(string msg, Exception ex = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Warn, name, msg);
            theEvent.Properties["linenumber"] = "line " + lineNumber;
            theEvent.Exception = ex;
            log.Log(theEvent);
        }
        public void Info(string msg, Exception ex = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Info, name, msg);
            theEvent.Properties["linenumber"] = "line " + lineNumber;
            theEvent.Exception = ex;
            log.Log(theEvent);
        }
        public void Debug(string msg, Exception ex = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Debug, name, msg);
            theEvent.Properties["linenumber"] = "line " + lineNumber;
            theEvent.Exception = ex;
            log.Log(theEvent);
        }
        public void Trace(string msg, Exception ex = null, [CallerLineNumber] int lineNumber = 0)
        {
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Trace, name, msg);
            theEvent.Properties["linenumber"] = "line " + lineNumber;
            theEvent.Exception = ex;
            log.Log(theEvent);
        }
    }

}
