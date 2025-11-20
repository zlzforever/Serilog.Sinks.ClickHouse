namespace Serilog.Sinks.ClickHouse;

/// <summary>
///
/// </summary>
public class ClickHouseOptions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpointAddr"></param>
    /// <param name="user"></param>
    /// <param name="key"></param>
    /// <param name="database"></param>
    /// <param name="table"></param>
    /// <param name="application"></param>
    /// <param name="skipServerCertificateValidation"></param>
    /// <param name="timeout"></param>
    public ClickHouseOptions(string endpointAddr, string user, string key, string database, string table,
        string? application, bool skipServerCertificateValidation, int timeout = 30)
    {
        User = user;
        Key = key;
        EndpointAddr = endpointAddr;
        Database = database;
        Table = table;
        Application = application;
        SkipServerCertificateValidation = skipServerCertificateValidation;
        Timeout = timeout;
    }

    /// <summary>
    ///
    /// </summary>
    public string User { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string EndpointAddr { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string Database { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string Table { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string? Application { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public bool SkipServerCertificateValidation { get; private set; }

    /// <summary>
    /// Second
    /// </summary>
    public int Timeout { get; private set; }
}