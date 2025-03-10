using System.Text;
using System.Text.Json;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.ClickHouse;

/// <summary>
///
/// </summary>
public class ClickHouseSink : IBatchedLogEventSink
{
    private static readonly HttpClient HttpClient = new();
    private readonly ClickHouseOptions _options;
    private readonly string _uri;

    /// <summary>
    ///
    /// </summary>
    public ClickHouseSink(ClickHouseOptions options)
    {
        _options = options;
        _uri = $"{options.EndpointAddr}/?query=INSERT+INTO+{options.Table}+FORMAT+JSONEachRow";
    }

    /// <summary>
    ///
    /// </summary>
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        var bodyBuilder = new StringBuilder();
        try
        {
            foreach (var logEvent in batch)
            {
                var ckEvent = new ClickHouseLogEvent(_options.Application, logEvent);
                bodyBuilder.Append(ckEvent).Append(' ').AppendLine();
            }

            var json = bodyBuilder.ToString();
            using var request = new HttpRequestMessage(HttpMethod.Post, _uri);
            request.Headers.TryAddWithoutValidation("X-ClickHouse-User", _options.User);
            request.Headers.TryAddWithoutValidation("X-ClickHouse-Key", _options.Key);
            request.Headers.TryAddWithoutValidation("X-ClickHouse-Database", _options.Database);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await HttpClient.SendAsync(request);
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine(await resp.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} {ex.StackTrace}");
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
}
