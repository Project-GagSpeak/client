using Serilog.Events;

namespace OtterGui.Log;

public readonly struct LazyString(Func<string> func)
{
    public static explicit operator LazyString(Func<string> func)
        => new(func);

    public override string ToString()
        => func();
}

public class Logger
{
    private readonly Serilog.ILogger _pluginLogger;
    private readonly string          _pluginName;
    private readonly string          _prefix;

    public Serilog.ILogger MainLogger
        => _pluginLogger;

    public Logger()
    {
        _pluginName   = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
        _pluginLogger = Serilog.Log.ForContext("Dalamud.PluginName", _pluginName);
        _prefix       = $"[{_pluginName}] ";
    }

    public enum LogLevel
    {
        Excessive   = LogEventLevel.Verbose,
        Verbose     = LogEventLevel.Verbose,
        Debug       = LogEventLevel.Debug,
        Information = LogEventLevel.Information,
        Warning     = LogEventLevel.Warning,
        Error       = LogEventLevel.Error,
        Fatal       = LogEventLevel.Fatal,
    }

    [InterpolatedStringHandler]
    public ref struct LogLevelInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public LogLevelInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, LogLevel level, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled((LogEventLevel) level);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct FatalInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public FatalInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled(LogEventLevel.Fatal);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct ErrorInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public ErrorInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled(LogEventLevel.Error);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct WarningInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public WarningInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled(LogEventLevel.Warning);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct InformationInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public InformationInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled(LogEventLevel.Information);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct DebugInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public DebugInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled(LogEventLevel.Debug);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    [InterpolatedStringHandler]
    public ref struct VerboseInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _builder;

        public VerboseInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
        {
            isEnabled = logger._pluginLogger.IsEnabled(LogEventLevel.Verbose);
            if (isEnabled)
            {
                _builder = new DefaultInterpolatedStringHandler(literalLength + logger._pluginName.Length + 3, formattedCount);
                _builder.AppendLiteral(logger._prefix);
            }
            else
            {
                _builder = default;
            }
        }

        public void AppendLiteral(string s)
            => _builder.AppendLiteral(s);

        public void AppendFormatted<T>(T t)
            => _builder.AppendFormatted(t);

        public void AppendFormatted<T>(T t, string format) where T : IFormattable
            => _builder.AppendFormatted(t, format);

        public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
            => _builder.AppendFormatted(t, alignment);

        public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
            => _builder.AppendFormatted(t, alignment, format);

        internal string GetFormattedText()
            => _builder.ToStringAndClear();
    }

    public void Message(LogLevel level, string text)
        => Message(level, $"{text}");

    public void Message(LogLevel level, [InterpolatedStringHandlerArgument("", "level")] LogLevelInterpolatedStringHandler builder)
    {
        if (Serilog.Log.IsEnabled((LogEventLevel)level))
            Serilog.Log.Write((LogEventLevel)level, builder.GetFormattedText());
    }

    public void Fatal(string text)
        => Fatal($"{text}");

    public void Fatal([InterpolatedStringHandlerArgument("")] FatalInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Fatal))
            _pluginLogger.Fatal(builder.GetFormattedText());
    }

    public void Error(string text)
        => Error($"{text}");

    public void Error([InterpolatedStringHandlerArgument("")] ErrorInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Error))
            _pluginLogger.Error(builder.GetFormattedText());
    }

    public void Warning(string text)
        => Warning($"{text}");

    public void Warning([InterpolatedStringHandlerArgument("")] WarningInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Warning))
            _pluginLogger.Warning(builder.GetFormattedText());
    }

    public void Information(string text)
        => Information($"{text}");

    public void Information([InterpolatedStringHandlerArgument("")] InformationInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Information))
            _pluginLogger.Information(builder.GetFormattedText());
    }

    public void Debug(string text)
        => Debug($"{text}");

    public void Debug([InterpolatedStringHandlerArgument("")] DebugInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Debug))
            _pluginLogger.Debug(builder.GetFormattedText());
    }

    public void Verbose(string text)
        => Verbose($"{text}");

    public void Verbose(string format, params object?[] args)
        => _pluginLogger.Verbose(_prefix + format, args);

    public void Verbose([InterpolatedStringHandlerArgument("")] VerboseInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Verbose))
            _pluginLogger.Verbose(builder.GetFormattedText());
    }

    [Conditional("EXCESSIVE_LOGGING")]
    public void Excessive(string text)
        => Verbose($"{text}");

    [Conditional("EXCESSIVE_LOGGING")]
    public void Excessive(string format, params object?[] args)
        => _pluginLogger.Verbose(_prefix + format, args);

    [Conditional("EXCESSIVE_LOGGING")]
    public void Excessive([InterpolatedStringHandlerArgument("")] VerboseInterpolatedStringHandler builder)
    {
        if (_pluginLogger.IsEnabled(LogEventLevel.Verbose))
            _pluginLogger.Verbose(builder.GetFormattedText());
    }
}
