# Serilog.Sinks.ClickHouse

Write log to ClickHouse

[![.NET](https://github.com/zlzforever/Serilog.Sinks.ClickHouse/actions/workflows/dotnet.yml/badge.svg)](https://github.com/zlzforever/Serilog.Sinks.ClickHouse/actions/workflows/dotnet.yml)

### Explore via grafana

![explore](https://github.com/zlzforever/Serilog.Sinks.ClickHouse/blob/main/explore.png)

### Create CK log table

``` sql
create table if not exists application_log
(
    _timestamp             DateTime64(3) CODEC (DoubleDelta, LZ4),
    application LowCardinality(String),
    source_context         String default '',
    -- 日志级别
    `level` LowCardinality(String),
    -- 键值使用一对 Array，查询效率相比 Map 会有很大提升,
    `message`              String default '',
    `exception_type`       String default '',
    `exception_message`    String default '' CODEC (ZSTD(1)),
    `exception_stacktrace` String default '' CODEC (ZSTD(1)),
    trace_id               String default '',
    span_id                String default '',
    `string_keys`          Array(String),
    `string_values`        Array(String),
    `number_keys`          Array(String),
    `number_values`        Array(Float64),
    `bool_keys`            Array(String),
    `bool_values`          Array(bool),
    `raw`                  String default '' CODEC (ZSTD(1)),
    -- 建立索引加速低命中率内容的查询
    INDEX idx_string_values `string_values` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 1,
    INDEX idx_raw raw TYPE tokenbf_v1(8192, 2, 0) GRANULARITY 1,
    INDEX idx_message `message` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 1,
    INDEX idx_exception_message `exception_message` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 1,
    INDEX idx_exception_stacktrace `exception_stacktrace` TYPE tokenbf_v1(4096, 2, 0) GRANULARITY 1,
-- 使用 Projection 记录 application 的数量，时间范围，列名等信息
    PROJECTION p_projects_usually (SELECT application,
                                          count(),
                                          min(_timestamp),
                                          max(_timestamp),
                                          groupUniqArrayArray(string_keys),
                                          groupUniqArrayArray(number_keys),
                                          groupUniqArrayArray(bool_keys)
                                   GROUP BY application)

) ENGINE = MergeTree
      PARTITION BY toYYYYMMDD(_timestamp)
      ORDER BY (application, _timestamp)
      SETTINGS index_granularity = 8192, -- 100MB 以上使用 Wide 格式
          min_bytes_for_wide_part = 104857600;
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
          "key": "xxxxx",
          "includeRaw": true
        }
      }
    ]
  }
}

```
 
