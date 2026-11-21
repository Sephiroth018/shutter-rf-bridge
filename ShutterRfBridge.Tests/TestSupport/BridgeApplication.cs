using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShutterRfBridge.Transmission;

namespace ShutterRfBridge.Tests.TestSupport;

/// <summary>
///     The service in-process, in the Development environment with its dry-run transmitter, made-up remote ID and
///     localhost allow-list. The <see cref="TransmitWorker" /> is left out, so the received commands stay in the
///     <see cref="TransmitQueue" /> for the test to take.
/// </summary>
internal sealed class BridgeApplication : WebApplicationFactory<Program>
{
    private readonly List<ServiceDescriptor> _replacedServices = [];
    private readonly Dictionary<string, string?> _settings = [];

    public TransmitQueue Queue => Services.GetRequiredService<TransmitQueue>();

    /// <summary>Overrides a configuration value; call before the first request.</summary>
    public void SetSetting(string key, string? value)
    {
        _settings[key] = value;
    }

    /// <summary>Registers <paramref name="instance" /> in place of the service's own; call before the first request.</summary>
    public void ReplaceService<TService>(TService instance) where TService : class
    {
        _replacedServices.Add(ServiceDescriptor.Singleton(instance));
    }

    /// <summary>Sends a <c>POST</c> without a body from <paramref name="client" /> and returns the status code.</summary>
    public async Task<int> PostAsync(string path, IPAddress? client)
    {
        var context = await Server.SendAsync(request =>
        {
            request.Request.Method = HttpMethods.Post;
            request.Request.Path = path;
            request.Connection.RemoteIpAddress = client;
        }, TestContext.Current.CancellationToken);
        return context.Response.StatusCode;
    }

    /// <inheritdoc cref="PostAsync(string, IPAddress?)" />
    public Task<int> PostAsync(string path) => PostAsync(path, IPAddress.Loopback);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        builder.ConfigureServices(services =>
        {
            services.Remove(services.Single(service => service.ImplementationType == typeof(TransmitWorker)));
            foreach (var service in _replacedServices)
            {
                services.Add(service);
            }
        });
    }
}
