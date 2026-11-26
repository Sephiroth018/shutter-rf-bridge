using System.Net;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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

    /// <summary>
    ///     Makes the substitute answer every request with <paramref name="status" /> and <paramref name="body" />,
    ///     handing each request to <paramref name="onRequest" /> first.
    /// </summary>
    public void Respond(HttpStatusCode status, string body, Action<HttpRequestMessage>? onRequest = null)
    {
        SendRequestAsync(Arg.Do<HttpRequestMessage>(request => onRequest?.Invoke(request)),
                Arg.Any<CancellationToken>())
            .Returns(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    /// <summary>Makes the substitute fail every request with <paramref name="exception" />.</summary>
    public void Fail(Exception exception)
    {
        SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).ThrowsAsync(exception);
    }

    protected sealed override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        SendRequestAsync(request, cancellationToken);
}
