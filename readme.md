# Serilog.Sinks.ClickHouse

Write log to ClickHouse

[![.NET](https://github.com/zlzforever/Serilog.Sinks.ClickHouse/actions/workflows/dotnet.yml/badge.svg)](https://github.com/zlzforever/Serilog.Sinks.ClickHouse/actions/workflows/dotnet.yml)

### Explore via grafana

![explore](https://github.com/zlzforever/Serilog.Sinks.ClickHouse/blob/main/explore.png)

### Create CK log table

``` sql
create table if not exists application_log
(
    _timestamp             DateTime64(3) DEFAULT now() CODEC (DoubleDelta, LZ4),
    application LowCardinality(String),
    source_context         String,
    -- 日志级别
    `level` LowCardinality(String),
    -- 键值使用一对 Array，查询效率相比 Map 会有很大提升,
    `message`              String        DEFAULT '',
    `exception_type`       String        DEFAULT '',
    `exception_message`    String        DEFAULT '',
    `exception_stacktrace` String        DEFAULT '',
    trace_id Nullable (String) CODEC (ZSTD(1)),
    span_id Nullable (String) CODEC (ZSTD(1)),
    `string_keys`          Array(String),
    `string_values`        Array(String),
    `number_keys`          Array(String),
    `number_values`        Array(Float64),
    `bool_keys`            Array(String),
    `bool_values`          Array(bool),
    -- 建立索引加速低命中率内容的查询
    INDEX idx_string_values `string_values` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2,
    INDEX idx_message `message` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2,
    INDEX idx_exception_message `exception_message` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2,
    INDEX idx_exception_stacktrace `exception_stacktrace` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 2
)
    ENGINE = MergeTree
        PARTITION BY toYYYYMMDD(_timestamp)
        ORDER BY (application, _timestamp)
        SETTINGS index_granularity = 8192;
```

### Register ClickHouse sink

```csharp
            Log.Logger = new LoggerConfiguration().ReadFrom
                .Configuration(configuration).WriteTo
                .ClickHouse("http://localhost:8123", "logs", "application_log",
                    "default", "xxx", "ordering-api")
                .CreateLogger();
``` 

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.ClickHouse"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "ClickHouse",
        "Args": {
          "endpointAddr": "http://localhost:8123",
          "database": "logs",
          "table": "application_log",
          "application": "ordering-api",
          "user": "default",
          "key": "xxxxx"
        }
      }
    ]
  }
}

```
 
