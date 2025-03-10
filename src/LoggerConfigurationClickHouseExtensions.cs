using Serilog.Configuration;
using Serilog.Events;

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
    /// create table if not exists application_log
    /// (
    ///     _timestamp             DateTime64(3) DEFAULT now() CODEC (DoubleDelta, LZ4),
    ///     application LowCardinality(String),
    ///     source_context         String,
    ///     -- 日志级别
    ///     `level` LowCardinality(String),
    ///     -- 键值使用一对 Array，查询效率相比 Map 会有很大提升,
    ///     `message`              String        DEFAULT '',
    ///     `exception_type`       String        DEFAULT '',
    ///     `exception_message`    String        DEFAULT '',
    ///     `exception_stacktrace` String        DEFAULT '',
    ///     trace_id Nullable (String) CODEC (ZSTD(1)),
    ///     span_id Nullable (String) CODEC (ZSTD(1)),
    ///     `string_keys`          Array(String),
    ///     `string_values`        Array(String),
    ///     `number_keys`          Array(String),
    ///     `number_values`        Array(Float64),
    ///     `bool_keys`            Array(String),
    ///     `bool_values`          Array(bool),
    ///     -- 建立索引加速低命中率内容的查询
    ///     INDEX idx_string_values `string_values` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2,
    ///     INDEX idx_message `message` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2,
    ///     INDEX idx_exception_message `exception_message` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2,
    ///     INDEX idx_exception_stacktrace `exception_stacktrace` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2
    /// )
    ///     ENGINE = MergeTree
    ///         PARTITION BY toYYYYMMDD(_timestamp)
    ///         ORDER BY (application, _timestamp)
    ///         SETTINGS index_granularity = 8192;
    /// </summary>
    public static LoggerConfiguration ClickHouse(
        this LoggerSinkConfiguration loggerConfiguration,
        string endpointAddr,
        string database,
        string table,
        string user,
        string key,
        string? application,
        TimeSpan? period = null,
        int batchSizeLimit = DefaultBatchSizeLimit,
        int queueLimit = DefaultQueueLimit,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
    {
        var options = new ClickHouseOptions
        {
            Application = application,
            Database = database,
            Table = table,
            User = user,
            Key = key,
            EndpointAddr = endpointAddr
        };
        var sink = new ClickHouseSink(options);
        var batchingOptions = new BatchingOptions
        {
            BatchSizeLimit = batchSizeLimit, BufferingTimeLimit = period ?? DefaultPeriod, QueueLimit = queueLimit
        };
        return loggerConfiguration.Sink(sink, batchingOptions, restrictedToMinimumLevel);
    }
}