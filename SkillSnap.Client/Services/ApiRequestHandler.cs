using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace SkillSnap.Client.Services;

public sealed class ApiRequestHandler : DelegatingHandler
{
    private readonly AntiforgeryTokenStore tokenStore;

    public ApiRequestHandler(AntiforgeryTokenStore tokenStore)
    {
        this.tokenStore = tokenStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        if (IsUnsafe(request.Method) &&
            !string.IsNullOrWhiteSpace(tokenStore.Token))
        {
            request.Headers.TryAddWithoutValidation(
                "X-CSRF-TOKEN",
                tokenStore.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsUnsafe(HttpMethod method)
    {
        return method != HttpMethod.Get &&
               method != HttpMethod.Head &&
               method != HttpMethod.Options &&
               method != HttpMethod.Trace;
    }
}