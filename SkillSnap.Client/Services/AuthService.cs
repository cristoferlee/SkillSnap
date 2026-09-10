using System.Net.Http.Json;
using SkillSnap.Client.Models;
using LoginContract = SkillSnap.Contracts.Auth.LoginRequest;
using SessionResponse = SkillSnap.Contracts.Auth.SessionResponse;

namespace SkillSnap.Client.Services;

public sealed class AuthService
{
    private readonly HttpClient httpClient;
    private readonly AntiforgeryTokenStore tokenStore;
    private readonly UserSessionService userSession;
    private Task? initializationTask;

    public AuthService(
        HttpClient httpClient,
        AntiforgeryTokenStore tokenStore,
        UserSessionService userSession)
    {
        this.httpClient = httpClient;
        this.tokenStore = tokenStore;
        this.userSession = userSession;
    }

    public bool IsAuthenticated { get; private set; }

    public string? Email { get; private set; }

    public IReadOnlyList<string> Roles { get; private set; } = [];

    public event Action? AuthenticationChanged;

    public bool IsInRole(string role)
    {
        return Roles.Contains(
            role,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(tokenStore.Token))
        {
            await RefreshSessionAsync();
        }

        var response = await httpClient.PostAsJsonAsync(
            "api/auth/login",
            new LoginContract
            {
                Email = request.Email,
                Password = request.Password
            });

        response.EnsureSuccessStatusCode();

        var session = await response.Content
            .ReadFromJsonAsync<SessionResponse>()
            ?? throw new InvalidOperationException(
                "The login response was empty.");

        ApplySession(session);
    }

    public Task InitializeAsync()
{
    return initializationTask ??= InitializeCoreAsync();
}

private async Task InitializeCoreAsync()
{
    try
    {
        await RefreshSessionAsync();
    }
    catch (HttpRequestException)
    {
        ClearSession();

        // Allow a later retry if the API was temporarily unavailable.
        initializationTask = null;
    }
}

    public async Task LogoutAsync()
    {
        try
        {
            var response = await httpClient.PostAsync(
                "api/auth/logout",
                content: null);

            response.EnsureSuccessStatusCode();

            var session = await response.Content
                .ReadFromJsonAsync<SessionResponse>();

            if (session is not null)
            {
                ApplySession(session);
                return;
            }
        }
        catch (HttpRequestException)
        {
            // Local state must still be cleared if the API is unavailable.
        }

        ClearSession();
    }

    private async Task RefreshSessionAsync()
    {
        var session = await httpClient
            .GetFromJsonAsync<SessionResponse>(
                "api/auth/session")
            ?? throw new InvalidOperationException(
                "The session response was empty.");

        ApplySession(session);
    }

    private void ApplySession(SessionResponse session)
    {
        tokenStore.Set(session.AntiforgeryToken);

        IsAuthenticated = session.IsAuthenticated;
        Email = session.Email;
        Roles = session.Roles;

        if (session.IsAuthenticated)
        {
            userSession.SetUser(
                session.UserId,
                session.Email,
                session.Roles.FirstOrDefault());
        }
        else
        {
            userSession.Clear();
        }

        AuthenticationChanged?.Invoke();
    }

    private void ClearSession()
    {
        tokenStore.Clear();
        IsAuthenticated = false;
        Email = null;
        Roles = [];
        userSession.Clear();
        AuthenticationChanged?.Invoke();
    }
}