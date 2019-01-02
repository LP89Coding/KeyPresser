using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyPresser
{
    public class Logger
    {
        private static NLog.Logger LoggerInstance = null;
        private static bool InitializationFailed = false;
        private static bool Closed = false;

        public static void Initialize()
        {
            try
            {
                LoggerInstance = NLog.LogManager.GetCurrentClassLogger();
                Closed = false;
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                InitializationFailed = true;
            }
        }
        
        public static void Close()
        {
            try
            {
                NLog.LogManager.Shutdown();
                Closed = true;
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void Log(LogData logData, params object[] args)
        {
            if (logData != null && !InitializationFailed && !Closed)
            {
                if (LoggerInstance == null)
                    Initialize();
                NLog.LogEventInfo lei = new NLog.LogEventInfo(logData.LogLevel, LoggerInstance.Name, null, logData.Message, args);
                // this data can be retrieved using ${event-context:EventID}
                lei.Properties["EventID"] = logData.EventID;
                LoggerInstance.Log(lei);
            }
        }

        public class LogData
        {
            public long EventID { get; private set; }
            public NLog.LogLevel LogLevel { get; private set; }
            public string Message { get; private set; }

            public LogData(long EventID, NLog.LogLevel LogLevel, string Message)
            {
                this.EventID = EventID;
                this.LogLevel = LogLevel;
                this.Message = Message;
            }
        }

 
    }
}
