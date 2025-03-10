namespace Serilog.Sinks.ClickHouse;

/// <summary>
///
/// </summary>
public class ClickHouseOptions
{
    /// <summary>
    ///
    /// </summary>
    public string User { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string EndpointAddr { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string Database { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string Table { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string? Application { get; set; }
}