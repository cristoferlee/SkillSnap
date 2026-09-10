using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillSnap.Contracts.Auth;

namespace SkillSnap.Api.Tests;

public sealed class AuthSessionTests :
    IClassFixture<SkillSnapApiFactory>
{
    private readonly SkillSnapApiFactory factory;

    public AuthSessionTests(SkillSnapApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSession_WhenAnonymous_ReturnsAntiforgeryToken()
    {
        // Arrange
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        // Act
        var response = await client.GetAsync(
            "/api/auth/session");

        var session = await response.Content
            .ReadFromJsonAsync<SessionResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(session);
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.Email);
        Assert.Empty(session.Roles);
        Assert.False(
            string.IsNullOrWhiteSpace(
                session.AntiforgeryToken));
    }

    [Fact]
    public async Task Register_IsNotAvailable()
    {
        // Arrange
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = "visitor@example.com",
                Password = "NotUsed123!"
            });

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutAntiforgeryToken_ReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = "unknown@example.com",
                Password = "InvalidPassword123!"
            });

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var session = await client
            .GetFromJsonAsync<SessionResponse>(
                "/api/auth/session");

        Assert.NotNull(session);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/login");

        request.Headers.Add(
            "X-CSRF-TOKEN",
            session.AntiforgeryToken);

        request.Content = JsonContent.Create(
            new LoginRequest
            {
                Email = "unknown@example.com",
                Password = "InvalidPassword123!"
            });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidAdmin_ReturnsAuthenticatedSession()
    {
        // Arrange
        const string email = "admin@skillsnap.test";
        const string password = "TestPassword123!";

        await factory.CreateUserAsync(
            email,
            password,
            role: "Admin");

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var anonymousSession = await client
            .GetFromJsonAsync<SessionResponse>(
                "/api/auth/session");

        Assert.NotNull(anonymousSession);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/login");

        request.Headers.Add(
            "X-CSRF-TOKEN",
            anonymousSession.AntiforgeryToken);

        request.Content = JsonContent.Create(
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        // Act
        var response = await client.SendAsync(request);
        var authenticatedSession = await response.Content
            .ReadFromJsonAsync<SessionResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(authenticatedSession);
        Assert.True(authenticatedSession.IsAuthenticated);
        Assert.Equal(email, authenticatedSession.Email);
        Assert.Contains("Admin", authenticatedSession.Roles);

        var setCookieHeaders = response.Headers
            .GetValues("Set-Cookie");

        Assert.Contains(
            setCookieHeaders,
            value =>
                value.Contains(
                    "SkillSnap.Auth=",
                    StringComparison.OrdinalIgnoreCase) &&
                value.Contains(
                    "httponly",
                    StringComparison.OrdinalIgnoreCase));
    }
}
