using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Sinks.ClickHouse;
using Serilog.Sinks.ClickHouse.HttpDriver;

namespace Serilog.Sinks.Clickhouse.Tests;

public class UnitTest1
{
    [Fact]
    public void CreateTable()
    {
        var options = new ClickHouseOptions("http://192.168.100.254:8123", "default", "5%97SP%cYdD*m%", "logs",
            "application_log_test", "Serilog.Sinks.Clickhouse", true);

        var sink = new ClickHouseSink(options);
        sink.Initialize();
    }
    
    [Fact]
    public void CreateTableWithEmptyKey()
    {
        var options = new ClickHouseOptions("http://10.0.10.190:8123", "default", "", "logs",
            "application_log_test", "Serilog.Sinks.Clickhouse", true);

        var sink = new ClickHouseSink(options);
        sink.Initialize();
    }

    [Fact]
    public void FormatAllPropertyType()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        var traceId = ActivityTraceId.CreateFromString("0bddde7094c87906fa7fc43993e650e8");
        var spanId = ActivitySpanId.CreateFromString("a041dd2b4ecc5d39");

        var formatter = new ApplicationLogFormatter("app1");
        var properties = new object[]
        {
            "Lewis",
            1.1f,
            2.2d,
            3.3m,
            1000,
            (long)1000,
            DateTime.Parse("2025-11-19T16:31:57.4249140+08:00"),
            DateTimeOffset.Parse("2025-11-19T16:31:57.4249140+08:00"),
            true,
            new Dictionary<string, string>
            {
                { "k1", "v1" }
            },
            new[] { 1, 2, 3 }, new List<string> { "str1" },
            new Person
            {
                Age = 100,
                Name = "lewis zou"
            }
        };

        Log.BindMessageTemplate(Template,
            properties, out var parsedTemplate, out var boundProperties);
        var evt = new LogEvent(DateTimeOffset.Parse("2025-11-19T16:31:57.4249140+08:00"), LogEventLevel.Error,
            new ApplicationException("test"),
            parsedTemplate!, boundProperties!, traceId, spanId);

        var writer = new StringWriter();
        formatter.Format(evt, writer);
        var str = writer.ToString();

        var jobj = (JObject)JsonConvert.DeserializeObject(str)!;
        var raw = jobj["raw"]!.ToString();
        Assert.Equal("""
                     {"_timestamp":1763541117424,"level":"Error","message":"\"Lewis\" 1.1 2.2 3.3 1000 1000 11/19/2025 16:31:57 11/19/2025 16:31:57 +08:00 True [(\"k1\": \"v1\")] [1, 2, 3] [\"str1\"] \"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\"","trace_id":"0bddde7094c87906fa7fc43993e650e8","span_id":"a041dd2b4ecc5d39","exception_type":"System.ApplicationException","exception_message":"test","exception_stacktrace":"","application":"app1","string_keys":["String","Dictionary","Array","List","Object"],"string_values":["Lewis","{\"k1\":\"\\u0022v1\\u0022\"}","[\"1\",\"2\",\"3\"]","[\"\\u0022str1\\u0022\"]","Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person"],"bool_keys":["Boolean"],"bool_values":[true],"number_keys":["Float","Double","Decimal","Int32","Int64"],"number_values":[1.1,2.2,3.3,1000,1000]}
                     """, raw);
    }

    [Fact]
    public async Task SendToCk()
    {
        var json = """
                   {"_timestamp":1763541117424,"level":"Error","message":"\"Lewis\" 1.1 2.2 3.3 1000 1000 11/19/2025 16:31:57 11/19/2025 16:31:57 +08:00 True [(\"k1\": \"v1\")] [1, 2, 3] [\"str1\"] \"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\"","trace_id":"0bddde7094c87906fa7fc43993e650e8","span_id":"a041dd2b4ecc5d39","exception_type":"System.ApplicationException","exception_message":"test","exception_stacktrace":"","application":"app1","string_keys":["String","Dictionary","Array","List","Object"],"string_values":["Lewis","{\"k1\":\"\\u0022v1\\u0022\"}","[\"1\",\"2\",\"3\"]","[\"\\u0022str1\\u0022\"]","Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person"],"bool_keys":["Boolean"],"bool_values":[true],"number_keys":["Float","Double","Decimal","Int32","Int64"],"number_values":[1.1,2.2,3.3,1000,1000],"raw":"{\"_timestamp\":1763541117424,\"level\":\"Error\",\"message\":\"\\\"Lewis\\\" 1.1 2.2 3.3 1000 1000 11/19/2025 16:31:57 11/19/2025 16:31:57 +08:00 True [(\\\"k1\\\": \\\"v1\\\")] [1, 2, 3] [\\\"str1\\\"] \\\"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\\\"\",\"trace_id\":\"0bddde7094c87906fa7fc43993e650e8\",\"span_id\":\"a041dd2b4ecc5d39\",\"exception_type\":\"System.ApplicationException\",\"exception_message\":\"test\",\"exception_stacktrace\":\"\",\"application\":\"app1\",\"string_keys\":[\"String\",\"Dictionary\",\"Array\",\"List\",\"Object\"],\"string_values\":[\"Lewis\",\"{\\\"k1\\\":\\\"\\\\u0022v1\\\\u0022\\\"}\",\"[\\\"1\\\",\\\"2\\\",\\\"3\\\"]\",\"[\\\"\\\\u0022str1\\\\u0022\\\"]\",\"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\"],\"bool_keys\":[\"Boolean\"],\"bool_values\":[true],\"number_keys\":[\"Float\",\"Double\",\"Decimal\",\"Int32\",\"Int64\"],\"number_values\":[1.1,2.2,3.3,1000,1000]}"}
                   """;
        using HttpClient httpClient =
            new HttpClient(
                new DefaultHttpClientHandler("default", "5%97SP%cYdD*m%", "logs", true), false);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"http://192.168.100.254:8123/?query=INSERT+INTO+application_log_test+FORMAT+JSONEachRow");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await httpClient.SendAsync(request);
        var result = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();
        Assert.Equal("", result);
    }
    
        [Fact]
    public async Task SendToCkWithoutKey()
    {
        var json = """
                   {"_timestamp":1763541117424,"level":"Error","message":"\"Lewis\" 1.1 2.2 3.3 1000 1000 11/19/2025 16:31:57 11/19/2025 16:31:57 +08:00 True [(\"k1\": \"v1\")] [1, 2, 3] [\"str1\"] \"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\"","trace_id":"0bddde7094c87906fa7fc43993e650e8","span_id":"a041dd2b4ecc5d39","exception_type":"System.ApplicationException","exception_message":"test","exception_stacktrace":"","application":"app1","string_keys":["String","Dictionary","Array","List","Object"],"string_values":["Lewis","{\"k1\":\"\\u0022v1\\u0022\"}","[\"1\",\"2\",\"3\"]","[\"\\u0022str1\\u0022\"]","Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person"],"bool_keys":["Boolean"],"bool_values":[true],"number_keys":["Float","Double","Decimal","Int32","Int64"],"number_values":[1.1,2.2,3.3,1000,1000],"raw":"{\"_timestamp\":1763541117424,\"level\":\"Error\",\"message\":\"\\\"Lewis\\\" 1.1 2.2 3.3 1000 1000 11/19/2025 16:31:57 11/19/2025 16:31:57 +08:00 True [(\\\"k1\\\": \\\"v1\\\")] [1, 2, 3] [\\\"str1\\\"] \\\"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\\\"\",\"trace_id\":\"0bddde7094c87906fa7fc43993e650e8\",\"span_id\":\"a041dd2b4ecc5d39\",\"exception_type\":\"System.ApplicationException\",\"exception_message\":\"test\",\"exception_stacktrace\":\"\",\"application\":\"app1\",\"string_keys\":[\"String\",\"Dictionary\",\"Array\",\"List\",\"Object\"],\"string_values\":[\"Lewis\",\"{\\\"k1\\\":\\\"\\\\u0022v1\\\\u0022\\\"}\",\"[\\\"1\\\",\\\"2\\\",\\\"3\\\"]\",\"[\\\"\\\\u0022str1\\\\u0022\\\"]\",\"Serilog.Sinks.Clickhouse.Tests.UnitTest1+Person\"],\"bool_keys\":[\"Boolean\"],\"bool_values\":[true],\"number_keys\":[\"Float\",\"Double\",\"Decimal\",\"Int32\",\"Int64\"],\"number_values\":[1.1,2.2,3.3,1000,1000]}"}
                   """;
        using HttpClient httpClient =
            new HttpClient(
                new DefaultHttpClientHandler("default", "", "logs", true), false);
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"http://10.0.10.190:8123/?query=INSERT+INTO+application_log_test+FORMAT+JSONEachRow");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await httpClient.SendAsync(request);
        var result = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();
        Assert.Equal("", result);
    }

    [Fact]
    public void LogToCk()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.ClickHouse("http://192.168.100.254:8123", "logs", "application_log_test", "default",
                "5%97SP%cYdD*m%", "Serilog.Sinks.Clickhouse", batchSizeLimit: 1)
            .CreateLogger();

        var properties = new object[]
        {
            "Lewis",
            1.1f,
            2.2d,
            3.3m,
            1000,
            (long)1000,
            DateTime.Parse("2025-11-19T16:31:57.4249140+08:00"),
            DateTimeOffset.Parse("2025-11-19T16:31:57.4249140+08:00"),
            true,
            new Dictionary<string, string>
            {
                { "k1", "v1" }
            },
            new[] { 1, 2, 3 }, new List<string> { "str1" },
            new Person
            {
                Age = 100,
                Name = "lewis zou"
            }
        };

        Log.Logger.Information(Template, properties);
        Thread.Sleep(10000);
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private static readonly string Template =
        "{String} {Float} {Double} {Decimal} {Int32} {Int64} {DateTime} {DateTimeOffset} {Boolean} {Dictionary} {Array} {List} {Object}";
}