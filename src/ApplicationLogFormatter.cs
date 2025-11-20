using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.ClickHouse;

/// <summary>
/// 
/// </summary>
public class ApplicationLogFormatter
{
    private readonly string? _application;

    /// <summary>
    /// 
    /// </summary>
    private static readonly JsonSerializerOptions JsonSerializerOptions = new();

    static ApplicationLogFormatter()
    {
        JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    }

    /// <summary>
    /// Construct a <see cref="ApplicationLogFormatter"/>.
    /// </summary>
    /// <param name="application"></param>
    public ApplicationLogFormatter(string? application)
    {
        _application = application;
    }
    
    /// <summary>
    /// Format the log event into the output.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="logEvent"/> is <code>null</code></exception>
    /// <exception cref="ArgumentNullException">When <paramref name="output"/> is <code>null</code></exception>
    public void Format(LogEvent logEvent, StringWriter output)
    {
        output.Write("{\"_timestamp\":");
        output.Write(logEvent.Timestamp.ToUnixTimeMilliseconds());
        output.Write(",\"level\":\"");
        output.Write(logEvent.Level);
        output.Write("\"");

        output.Write(",\"message\":");
        var message = logEvent.MessageTemplate.Render(logEvent.Properties);
        JsonValueFormatter.WriteQuotedJsonString(message, output);

        if (logEvent.TraceId != null)
        {
            output.Write(",\"trace_id\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.TraceId.ToString(), output);
        }

        if (logEvent.SpanId != null)
        {
            output.Write(",\"span_id\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.SpanId.ToString(), output);
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"exception_type\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.GetType().FullName ?? "", output);

            output.Write(",\"exception_message\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.Message, output);

            output.Write(",\"exception_stacktrace\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.StackTrace ?? "", output);
        }

        output.Write(",\"application\":");
        if (_application == null)
        {
            output.Write("null");
        }
        else
        {
            JsonValueFormatter.WriteQuotedJsonString(_application, output);
        }

        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextPropertyValue))
        {
            output.Write(",\"source_context\":");
            if (sourceContextPropertyValue is not ScalarValue s)
            {
                output.Write("null");
            }
            else
            {
                if (s.Value is not string v)
                {
                    output.Write("null");
                }
                else
                {
                    JsonValueFormatter.WriteQuotedJsonString(v, output);
                }
            }
        }

        WriteStringKeys(logEvent.Properties, output);
        WriteStringValues(logEvent.Properties, output);

        WriteArray(logEvent.Properties, "bool_keys", value => value is bool, output,
            (key, _, writer) => JsonValueFormatter.WriteQuotedJsonString(key, writer));
        WriteArray(logEvent.Properties, "bool_values", value => value is bool, output,
            (_, value, writer) => writer.Write(true.Equals(value) ? "true" : "false"));

        WriteArray(logEvent.Properties, "number_keys", IsNumeric, output,
            (key, _, writer) => JsonValueFormatter.WriteQuotedJsonString(key, writer));
        WriteArray(logEvent.Properties, "number_values", IsNumeric, output);

        output.Write('}');

        var sb = output.GetStringBuilder();
        var raw = output.ToString();
        sb.Remove(sb.Length - 1, 1);
        output.Write(",\"raw\":");
        JsonValueFormatter.WriteQuotedJsonString(raw, output);
        output.Write('}');
    }

    private void WriteStringKeys(IReadOnlyDictionary<string, LogEventPropertyValue> logEventProperties,
        TextWriter output)
    {
        output.Write(",\"string_keys\":[");
        var first = true;
        foreach (var propertyValue in logEventProperties)
        {
            // 非 scalar 即 array/object/dict 全部转为 JSON 字符串
            if (propertyValue.Value is ScalarValue scalarValue)
            {
                if (scalarValue.Value is not string)
                {
                    continue;
                }

                WriteHead(ref first, output);
                JsonValueFormatter.WriteQuotedJsonString(propertyValue.Key, output);
            }
            else
            {
                WriteHead(ref first, output);
                JsonValueFormatter.WriteQuotedJsonString(propertyValue.Key, output);
            }
        }

        output.Write(']');
    }

    private void WriteStringValues(IReadOnlyDictionary<string, LogEventPropertyValue> logEventProperties,
        TextWriter output)
    {
        output.Write(",\"string_values\":[");
        var first = true;
        foreach (var propertyValue in logEventProperties)
        {
            // 非 scalar 即 array/object/dict 全部转为 JSON 字符串
            if (propertyValue.Value is ScalarValue scalarValue)
            {
                if (scalarValue.Value is not string stringValue)
                {
                    continue;
                }

                WriteHead(ref first, output);
                JsonValueFormatter.WriteQuotedJsonString(stringValue, output);
            }
            else if (propertyValue.Value is DictionaryValue dictionaryValue)
            {
                WriteHead(ref first, output);
                var stringValue = JsonSerializer.Serialize(
                    dictionaryValue.Elements.ToDictionary(x => x.Key.Value, y => y.Value.ToString()),
                    JsonSerializerOptions);
                JsonValueFormatter.WriteQuotedJsonString(stringValue, output);
            }
            else if (propertyValue.Value is SequenceValue sequenceValue)
            {
                var stringValue = JsonSerializer.Serialize(sequenceValue.Elements.Select(x => x.ToString()),
                    JsonSerializerOptions);
                WriteHead(ref first, output);
                JsonValueFormatter.WriteQuotedJsonString(stringValue, output);
            }
            // StructureValue
            else if (propertyValue.Value is StructureValue structureValue)
            {
                WriteHead(ref first, output);
                var stringValue = JsonSerializer.Serialize(
                    structureValue.Properties.ToDictionary(x => x.Name, x => x.Value.ToString()),
                    JsonSerializerOptions);
                JsonValueFormatter.WriteQuotedJsonString(stringValue, output);
            }
        }

        output.Write(']');
    }

    private void WriteArray(IReadOnlyDictionary<string, LogEventPropertyValue> logEventProperties,
        string key, Func<object, bool> predicate,
        TextWriter output, Action<string, object, TextWriter>? formatter = null)
    {
        output.Write(",\"");
        output.Write(key);
        output.Write("\":[");
        var first = true;
        foreach (var propertyValue in logEventProperties)
        {
            if (propertyValue.Value is not ScalarValue scalarValue)
            {
                continue;
            }

            var value = scalarValue.Value;

            // 值为空，无法判断数据类型，则不记录
            if (value == null)
            {
                continue;
            }

            if (!predicate(value))
            {
                continue;
            }

            WriteHead(ref first, output);

            if (formatter == null)
            {
                scalarValue.Render(output);
            }
            else
            {
                formatter(propertyValue.Key, value, output);
            }
        }

        output.Write(']');
    }

    private void WriteHead(ref bool first, TextWriter output)
    {
        if (first)
        {
            first = false;
            return;
        }

        output.Write(',');
    }

    private static bool IsNumeric(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        var typeCode = Type.GetTypeCode(obj.GetType());
        return typeCode switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16
                or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
            _ => false
        };
    }
}