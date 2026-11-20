using System.Net;
using Microsoft.Extensions.Options;
using ShutterRfBridge.Configuration;

namespace ShutterRfBridge.Api;

/// <summary>Answers <c>403 Forbidden</c> to every client not in <see cref="BridgeOptions.AllowedClients" />.</summary>
internal sealed partial class ClientAllowListMiddleware
{
    private readonly HashSet<IPAddress> _allowedClients;
    private readonly ILogger<ClientAllowListMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ClientAllowListMiddleware(
        RequestDelegate next,
        IOptions<BridgeOptions> options,
        ILogger<ClientAllowListMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedClients = options.Value.AllowedClients.Select(client => Normalize(IPAddress.Parse(client))).ToHashSet();
    }

    public Task InvokeAsync(HttpContext context)
    {
        var client = context.Connection.RemoteIpAddress;
        if (client is not null && _allowedClients.Contains(Normalize(client)))
        {
            return _next(context);
        }

        LogRefused(client);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    // Kestrel listening on IPv6 reports IPv4 clients as IPv4-mapped IPv6 addresses, and link-local clients with the
    // receiving interface's scope ID, which equality compares; rebuilding from the bytes drops it.
    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : new IPAddress(address.GetAddressBytes());

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refused a request from {Client}, which isn't allowed")]
    private partial void LogRefused(IPAddress? client);
}
