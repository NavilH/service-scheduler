using System.Net.Http.Headers;

namespace ServiceScheduler.Client.Auth;

public class JwtAuthHandler(TokenStore tokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tokenStore.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStore.Token);
        return base.SendAsync(request, cancellationToken);
    }
}
