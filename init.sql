create table application_log
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


