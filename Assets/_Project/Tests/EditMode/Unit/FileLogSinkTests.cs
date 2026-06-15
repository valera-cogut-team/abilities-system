using System.IO;
using Logger.Domain;
using Logger.Infrastructure;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class FileLogSinkTests
    {
        [Test]
        public void Write_AppendsFormattedLineToFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"avantajprim-log-test-{System.Guid.NewGuid():N}.log");

            try
            {
                using (var sink = new FileLogSink(path))
                {
                    sink.Write(LogEntryData.Create(LogLevel.Info, "hello", source: "Test"));
                    sink.Flush();
                }

                string text = File.ReadAllText(path);
                StringAssert.Contains("[Info]", text);
                StringAssert.Contains("[Test]", text);
                StringAssert.Contains("hello", text);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
