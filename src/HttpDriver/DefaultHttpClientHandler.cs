using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Serilog.Sinks.ClickHouse.HttpDriver;

internal class DefaultHttpClientHandler : HttpClientHandler
{
    private readonly string _user;
    private readonly string _key;
    private readonly string _database;

    public DefaultHttpClientHandler(string user, string key, string database, bool skipServerCertificateValidation)
    {
        _user = user;
        _key = key;
        _database = database;
        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        if (!skipServerCertificateValidation)
            return;
        ServerCertificateCustomValidationCallback =
            (Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>)((_, _, _, _) => true);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("X-ClickHouse-User", _user);
        request.Headers.TryAddWithoutValidation("X-ClickHouse-Key", _key);
        request.Headers.TryAddWithoutValidation("X-ClickHouse-Database", _database);
        return base.SendAsync(request, cancellationToken);
    }
}