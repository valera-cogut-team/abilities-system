using Logger.Application;
using Logger.Domain;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class LoggerConfigTests
    {
        [Test]
        public void CreateDefault_WithDebugLogs_UsesDebugLevel()
        {
            var config = LoggerConfig.CreateDefault(enableDebugLogs: true);

            Assert.AreEqual(LogLevel.Debug, config.MinLevel);
        }

        [Test]
        public void CreateDefault_WithoutDebugLogs_UsesPerformanceProfile()
        {
            var config = LoggerConfig.CreateDefault(enableDebugLogs: false);

            Assert.AreEqual(LogLevel.Warning, config.MinLevel);
            Assert.IsFalse(config.EnableFileLogging);
            Assert.IsFalse(string.IsNullOrWhiteSpace(config.FilePath));
        }

        [Test]
        public void CreatePerformance_DisablesFileLogging()
        {
            var config = LoggerConfig.CreatePerformance();

            Assert.AreEqual(LogLevel.Warning, config.MinLevel);
            Assert.IsFalse(config.EnableFileLogging);
        }
    }
}
