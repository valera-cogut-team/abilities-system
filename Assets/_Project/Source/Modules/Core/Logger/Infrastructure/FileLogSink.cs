using System;
using System.IO;
using Logger.Application;
using Logger.Domain;

namespace Logger.Infrastructure
{
    public sealed class FileLogSink : ILogSink, IDisposable
    {
        private readonly StreamWriter _writer;
        private int _entriesSinceFlush;

        public FileLogSink(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required.", nameof(filePath));

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            _writer = new StreamWriter(filePath, append: true) { AutoFlush = false };
        }

        public void Write(LogEntryData entry)
        {
            string source = string.IsNullOrEmpty(entry.Source) ? "Logger" : entry.Source;
            _writer.Write('[');
            _writer.Write(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            _writer.Write("][");
            _writer.Write(entry.Level);
            _writer.Write("][");
            _writer.Write(source);
            _writer.Write("] ");
            _writer.WriteLine(entry.Message);

            if (entry.Exception != null)
                _writer.WriteLine(entry.Exception);

            _entriesSinceFlush++;
            if (_entriesSinceFlush >= LoggerConstants.FlushEveryEntries)
                Flush();
        }

        public void Flush()
        {
            _writer.Flush();
            _entriesSinceFlush = 0;
        }

        public void Dispose()
        {
            Flush();
            _writer.Dispose();
        }
    }
}
