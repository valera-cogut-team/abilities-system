using Core;
using Logger.Application;
using Logger.Facade;
using Logger.Infrastructure;
using Zenject;

namespace Logger
{
    public sealed class LoggerModule : IModule
    {
        public string Name => "Logger";
        public string Version => "1.0.0";
        public string[] Dependencies => new string[0];
        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private ILoggerService _service;
        private ILoggerFacade _facade;
        private FileLogSink _fileSink;

        public void Initialize(IModuleContext context)
        {
            Initialize(context, LoggerConfig.CreateDefault(enableDebugLogs: true));
        }

        public void Initialize(IModuleContext context, LoggerConfig config)
        {
            _context = context;
            config ??= LoggerConfig.CreateDefault(enableDebugLogs: true);

            _service = new LoggerService();
            _service.AddSink(new UnityDebugSink());
            ApplyLoggerConfig(config);

            context.Container.Bind<ILoggerService>().FromInstance(_service).AsSingle();
            _facade = new LoggerFacade(_service);
            context.Container.Bind<ILoggerFacade>().FromInstance(_facade).AsSingle();
        }

        private void ApplyLoggerConfig(LoggerConfig config)
        {
            _service ??= new LoggerService();
            _service.SetMinLevel(config.MinLevel);

            if (_fileSink != null)
            {
                _service.RemoveSink(_fileSink);
                _fileSink.Dispose();
                _fileSink = null;
            }

            if (config.EnableFileLogging && !string.IsNullOrWhiteSpace(config.FilePath))
            {
                _fileSink = new FileLogSink(config.FilePath);
                _service.AddSink(_fileSink);
            }
        }

        public void Enable() => IsEnabled = true;
        public void Disable() => IsEnabled = false;

        public void Shutdown()
        {
            Disable();
            _service?.Flush();
            _fileSink?.Dispose();
            _fileSink = null;
            _service = null;
            _facade = null;
        }
    }
}
