using System.IO;
using Logger.Domain;
using UnityEngine;

namespace Logger.Application
{
    public sealed class LoggerConfig
    {
        public bool EnableFileLogging { get; set; }
        public string FilePath { get; set; }
        public LogLevel MinLevel { get; set; }

        public static LoggerConfig CreateDefault(bool enableDebugLogs) =>
            enableDebugLogs ? CreateDevelopment(enableDebugLogs) : CreatePerformance();

        public static LoggerConfig CreateDevelopment(bool enableDebugLogs)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool enableFile = true;
#else
            var enableFile = false;
#endif
            return new LoggerConfig
            {
                EnableFileLogging = enableFile,
                FilePath = GetDefaultFilePath(),
                MinLevel = enableDebugLogs ? LogLevel.Debug : LogLevel.Info
            };
        }

        public static LoggerConfig CreatePerformance() =>
            new LoggerConfig
            {
                EnableFileLogging = false,
                FilePath = GetDefaultFilePath(),
                MinLevel = LogLevel.Warning
            };

        public static string GetDefaultFilePath() =>
            Path.Combine(UnityEngine.Application.persistentDataPath, LoggerConstants.LogsSubfolder,
                LoggerConstants.LogFileName);
    }
}
