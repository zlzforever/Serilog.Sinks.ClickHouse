using System.Runtime.CompilerServices;
using Serilog.Configuration;
using Serilog.Events;

[assembly: InternalsVisibleTo("Serilog.Sinks.Clickhouse.Tests")]

namespace Serilog.Sinks.ClickHouse;

/// <summary>
///
/// </summary>
public static class LoggerConfigurationClickHouseExtensions
{
    /// <summary>
    ///     The default batch size limit.
    /// </summary>
    private const int DefaultBatchSizeLimit = 30;

    /// <summary>
    /// The default queue limit.
    /// </summary>
    private const int DefaultQueueLimit = int.MaxValue;

    /// <summary>
    ///     Default time to wait between checking for event batches.
    /// </summary>
    public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerConfiguration"></param>
    /// <param name="endpointAddr"></param>
    /// <param name="database"></param>
    /// <param name="table"></param>
    /// <param name="user"></param>
    /// <param name="key"></param>
    /// <param name="application"></param>
    /// <param name="period"></param>
    /// <param name="batchSizeLimit"></param>
    /// <param name="queueLimit"></param>
    /// <param name="restrictedToMinimumLevel"></param>
    /// <param name="timeout"></param>
    /// <param name="skipServerCertificateValidation"></param>
    /// <returns></returns>
    public static LoggerConfiguration ClickHouse(
        this LoggerSinkConfiguration loggerConfiguration,
        string endpointAddr,
        string database,
        string table,
        string user,
        string key,
        string? application = null,
        TimeSpan? period = null,
        int batchSizeLimit = DefaultBatchSizeLimit,
        int queueLimit = DefaultQueueLimit,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose, int timeout = 30,
        bool skipServerCertificateValidation = false)
    {
        var options = new ClickHouseOptions(endpointAddr, user, key, database, table, application,
            skipServerCertificateValidation, timeout);

        var sink = new ClickHouseSink(options);
        sink.Initialize();
        var batchingOptions = new BatchingOptions
        {
            BatchSizeLimit = batchSizeLimit, BufferingTimeLimit = period ?? DefaultPeriod, QueueLimit = queueLimit
        };
        return loggerConfiguration.Sink(sink, batchingOptions, restrictedToMinimumLevel);
    }
}