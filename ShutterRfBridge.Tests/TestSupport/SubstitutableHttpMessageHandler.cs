namespace ShutterRfBridge.Tests.TestSupport;

/// <summary>
///     An <see cref="HttpMessageHandler" /> for <c>Substitute.For</c>: its protected <c>SendAsync</c> forwards to the
///     public <see cref="SendRequestAsync" />, which a substitute can configure.
/// </summary>
public abstract class SubstitutableHttpMessageHandler : HttpMessageHandler
{
    public abstract Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);

    protected sealed override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        SendRequestAsync(request, cancellationToken);
}
