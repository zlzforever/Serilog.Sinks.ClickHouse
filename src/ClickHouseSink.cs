using System.Text;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.ClickHouse.HttpDriver;

namespace Serilog.Sinks.ClickHouse;

/// <summary>
///
/// </summary>
public class ClickHouseSink : IBatchedLogEventSink, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ClickHouseOptions _options;
    private readonly string _uri;
    private readonly ApplicationLogFormatter _textFormatter;

    /// <summary>
    /// 
    /// </summary>
    private const string QueryString =
        "?query=INSERT+INTO+application_log_test+FORMAT+JSONEachRow+SETTINGS+async_insert=1";

    /// <summary>
    ///
    /// </summary>
    public ClickHouseSink(ClickHouseOptions options)
    {
        _options = options;
        _textFormatter = new ApplicationLogFormatter(options.Application);
        var endpoint = new Uri(options.EndpointAddr);
        _uri = $"{endpoint}{QueryString}";
        _httpClient =
            new HttpClient(
                new DefaultHttpClientHandler(options.User, options.Key, options.Database,
                    options.SkipServerCertificateValidation), false)
            {
                Timeout = TimeSpan.FromSeconds(options.Timeout)
            };
    }

    /// <summary>
    ///
    /// </summary>
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        var bodyBuilder = new StringWriter();

        foreach (var logEvent in batch)
        {
            _textFormatter.Format(logEvent, bodyBuilder);
            await bodyBuilder.WriteAsync(Environment.NewLine);
        }

        var json = bodyBuilder.ToString();
        for (var i = 0; i < 5; ++i)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _uri);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _httpClient.SendAsync(request);
                if (resp.IsSuccessStatusCode)
                {
                    // 执行成功退出重试
                    break;
                }

                var result = await resp.Content.ReadAsStringAsync();
                SelfLog.WriteLine("Failed to write event: {0}", result);
                // 失败则暂停 100 毫秒
                await Task.Delay(100);
            }
            catch (TimeoutException te)
            {
                SelfLog.WriteLine("Write event timeout: {0}", te.Message);
                // 失败则暂停 100 毫秒
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(ex.ToString());
                // 失败则暂停 100 毫秒
                await Task.Delay(100);
            }
        }
    }

    /// <inheritdoc cref="IBatchedLogEventSink" />
    /// <summary>
    /// Allows sinks to perform periodic work without requiring additional threads or
    /// timers (thus avoiding additional flush/shut-down complexity).
    /// </summary>
    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Initialize()
    {
        var sql = $$"""
                    create table if not exists {{_options.Table}}
                    (
                        _timestamp           DateTime64(3),
                        application LowCardinality(String),
                        source_context       String default '',
                        level LowCardinality(String),
                        message              String default '',
                        exception_type       String default '',
                        exception_message    String default '',
                        exception_stacktrace String default '',
                        trace_id             String default '',
                        span_id              String default '',
                        string_keys          Array(String),
                        string_values        Array(String),
                        number_keys          Array(String),
                        number_values        Array(Float64),
                        bool_keys            Array(String),
                        bool_values          Array(Bool),
                        raw                  String default ''
                    )
                        engine = MergeTree PARTITION BY toYYYYMMDD(_timestamp)
                            ORDER BY (application, _timestamp)
                            SETTINGS index_granularity = 8192, min_bytes_for_wide_part = 104857600;
                    """;
        _httpClient.PostAsync(_options.EndpointAddr,
            new StringContent(sql, Encoding.UTF8, "plain/text"));
    }
}