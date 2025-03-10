using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Events;

namespace Serilog.Sinks.ClickHouse;

/// <summary>
///
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class ClickHouseLogEvent
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new();
    private LogEvent _logEvent;

    static ClickHouseLogEvent()
    {
        JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    }

    /// <summary>
    /// Creates <see cref="ClickHouseLogEvent"/> from <see cref="_logEvent"/>.
    /// </summary>
    /// <param name="application"></param>
    /// <param name="logEvent">
    /// A log event.
    /// </param>
    /// <param name="includeRaw"></param>
    public ClickHouseLogEvent(string? application, LogEvent logEvent, bool includeRaw)
    {
        _logEvent = logEvent ?? throw new ArgumentNullException(nameof(logEvent));

        Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        Application = application;
        Level = GetLevel(logEvent.Level);
        Message = logEvent.RenderMessage();
        TraceId = logEvent.TraceId?.ToString();
        SpanId = logEvent.SpanId?.ToString();
        SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sourceContext)
            ? (sourceContext as ScalarValue)?.Value?.ToString()
            : null;
        if (logEvent.Exception != null)
        {
            ExceptionType = logEvent.Exception.GetType().FullName;
            ExceptionMessage = logEvent.Exception.Message;
            ExceptionStackTrace = logEvent.Exception.StackTrace;
        }

        StringKeys = new List<string>();
        StringValues = new List<string>();
        BoolKeys = new List<string>();
        BoolValues = new List<bool>();
        NumberKeys = new List<dynamic>();
        NumberValues = new List<dynamic>();

        var dict = includeRaw ? null : new Dictionary<string, dynamic>();
        foreach (var propertyValue in logEvent.Properties)
        {
            if (propertyValue.Value is ScalarValue scalarValue)
            {
                if (scalarValue.Value is null)
                {
                    continue;
                }

                object? vvv;
                switch (scalarValue.Value)
                {
                    case bool b:
                        AddBool(propertyValue.Key, b);
                        vvv = b;
                        break;
                    case string s:
                        AddString(propertyValue.Key, s);
                        vvv = s;
                        break;
                    default:
                    {
                        if (IsNumeric(scalarValue.Value))
                        {
                            AddNumber(propertyValue.Key, scalarValue.Value);
                            vvv = scalarValue.Value;
                        }
                        else if (scalarValue.Value.GetType().IsValueType)
                        {
                            var vv = scalarValue.Value.ToString() ?? "";
                            AddString(propertyValue.Key, vv);
                            vvv = vv;
                        }
                        else
                        {
                            var vv = JsonSerializer.Serialize(scalarValue.Value,
                                JsonSerializerOptions);
                            AddString(propertyValue.Key, vv);
                            vvv = vv;
                        }

                        break;
                    }
                }

                dict?.Add(propertyValue.Key, vvv);
            }
            else
            {
                AddString(propertyValue.Key, propertyValue.Value.ToString());
            }
        }

        if (dict != null)
        {
            if (!dict.ContainsKey("MessageTemplate"))
            {
                dict.Add("MessageTemplate", logEvent.MessageTemplate.Text);
            }
        }

        if (includeRaw)
        {
            Raw = JsonSerializer.Serialize(dict, JsonSerializerOptions);
        }
    }

    /// <summary>
    /// Internal event timestamp, created when event is emitted to the sink.
    /// </summary>
    [JsonPropertyName("_timestamp")]
    public long Timestamp { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("application")]
    public string? Application { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("source_context")]
    public string? SourceContext { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("span_id")]
    public string? SpanId { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("string_keys")]
    // ReSharper disable once CollectionNeverQueried.Global
    public List<string> StringKeys { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("string_values")]
    // ReSharper disable once CollectionNeverQueried.Global
    public List<string> StringValues { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("bool_keys")]
    // ReSharper disable once CollectionNeverQueried.Global
    public List<string> BoolKeys { get; private set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("bool_values")]
    // ReSharper disable once CollectionNeverQueried.Global
    public List<bool> BoolValues { get; private set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("number_keys")]
    // ReSharper disable once CollectionNeverQueried.Global
    public List<dynamic> NumberKeys { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("number_values")]
    public List<dynamic> NumberValues { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("exception_type")]
    public string? ExceptionType { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("exception_message")]
    public string? ExceptionMessage { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("exception_stacktrace")]
    public string? ExceptionStackTrace { get; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("raw")]
    public string? Raw { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddString(string key, string value)
    {
        StringKeys.Add(key);
        StringValues.Add(value);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddBool(string key, bool value)
    {
        BoolKeys.Add(key);
        BoolValues.Add(value);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddNumber(string key, dynamic value)
    {
        NumberKeys.Add(key);
        NumberValues.Add(value);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this,
            JsonSerializerOptions);
    }

    private string GetLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Debug => "debug",
            LogEventLevel.Error => "error",
            LogEventLevel.Fatal => "fatal",
            LogEventLevel.Information => "info",
            LogEventLevel.Verbose => "verbose",
            LogEventLevel.Warning => "warning",
            _ => "unknown"
        };
    }

    internal ClickHouseLogEvent CopyWithProperties(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties)
    {
        _logEvent = new LogEvent(
            _logEvent.Timestamp,
            _logEvent.Level,
            _logEvent.Exception,
            _logEvent.MessageTemplate,
            properties.Select(p => new LogEventProperty(p.Key, p.Value)));

        return this;
    }

    private static bool IsNumeric(object obj)
    {
        var typeCode = Type.GetTypeCode(obj.GetType());
        return typeCode switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16
                or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
            _ => false
        };
    }
}