using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogData = KeyPresser.Logger.LogData;

namespace KeyPresser
{
    public class EventID
    {
        //KeyPresser 1-100
        public static LogData KeyPresserStart = new LogData(1, NLog.LogLevel.Info, "KeyPresser start");
        public static LogData KeyPresserEnd = new LogData(2, NLog.LogLevel.Info, "KeyPresser end");
        public static LogData KeyPresserException = new LogData(3, NLog.LogLevel.Error, "KeyPresser Error: {0}");
        public static LogData KeyPresserUnhandledException = new LogData(4, NLog.LogLevel.Fatal, "Unhandled error: {0}");
        public static LogData KeyPresserUnhandledExceptionException = new LogData(5, NLog.LogLevel.Fatal, "Unhandled error error: {0}");

        //Simulation 101-200
        public static LogData SimulationStart = new LogData(101, NLog.LogLevel.Info, "Simulation start. IsHazardBehaviour: {0}");
        public static LogData SimulationEnd = new LogData(102, NLog.LogLevel.Info, "Simulation end. Time: {0}, ClickCount: {1}");
        public static LogData SimulationException = new LogData(103, NLog.LogLevel.Error, "Simulation Error: {0}");
        public static LogData SimulationAddedKey = new LogData(104, NLog.LogLevel.Debug, "Added key. Name: {0}, Code: {1}, IsActive: {2}, Frequency: {3}");
        public static LogData SimulationKey = new LogData(105, NLog.LogLevel.Debug, "Simulation key. Name: {0}, Code: {1}, Frequency: {2}");
        public static LogData SimulationTaskStart = new LogData(106, NLog.LogLevel.Info, "Simulation task start.");
        public static LogData SimulationTaskEnd = new LogData(107, NLog.LogLevel.Info, "Simulation task end.");

        //KeyPressInfo 201-300
        public static LogData KeyPressInfoException = new LogData(201, NLog.LogLevel.Error, "KeyPressInfo Error. IdKeyPressInfo: {0}, KeyName: {1}, Error: {2}");
        public static LogData KeyPressKeyPressed = new LogData(202, NLog.LogLevel.Trace, "KeyPresse. IdKeyPressInfo: {0}, KeyName: {1}, Key: {2}");

        //SendEmail 301-400
        public static LogData SendEmailException = new LogData(301, NLog.LogLevel.Error, "SendEmail Error: {0}");
    }
}
