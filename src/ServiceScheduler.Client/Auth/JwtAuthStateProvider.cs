using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ServiceScheduler.Client.Auth;

public class JwtAuthStateProvider(IJSRuntime js, TokenStore tokenStore) : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        tokenStore.Token = token;
        return new AuthenticationState(new ClaimsPrincipal(ParseToken(token)));
    }

    public async Task LoginAsync(string token)
    {
        await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        tokenStore.Token = token;
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(ParseToken(token)))));
    }

    public async Task LogoutAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        tokenStore.Token = null;
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static ClaimsIdentity ParseToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return new ClaimsIdentity(jwt.Claims, "jwt");
    }
}
