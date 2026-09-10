using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillSnap.Contracts.Auth;
using SkillSnap.Contracts.Contact;

namespace SkillSnap.Api.Tests;

public sealed class ContactEndpointTests :
    IClassFixture<SkillSnapApiFactory>
{
    private readonly SkillSnapApiFactory factory;

    public ContactEndpointTests(
        SkillSnapApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Submit_WithoutAntiforgeryToken_ReturnsBadRequest()
    {
        var client = CreateClient();
        var messagesBefore =
            await factory.CountContactMessagesAsync();

        var response = await client.PostAsJsonAsync(
            "/api/contact",
            CreateValidRequest());

        var messagesAfter =
            await factory.CountContactMessagesAsync();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(messagesBefore, messagesAfter);
    }

    [Fact]
    public async Task Submit_WithValidRequest_ReturnsAcceptedAndPersists()
    {
        var client = CreateClient();
        var session = await GetSessionAsync(client);
        var messagesBefore =
            await factory.CountContactMessagesAsync();

        using var request = CreatePostRequest(
            "/api/contact",
            session.AntiforgeryToken,
            CreateValidRequest());

        var response = await client.SendAsync(request);
        var messagesAfter =
            await factory.CountContactMessagesAsync();

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        Assert.Equal(
            messagesBefore + 1,
            messagesAfter);
    }

    [Fact]
    public async Task Submit_WithHoneypot_ReturnsAcceptedWithoutPersisting()
    {
        var client = CreateClient();
        var session = await GetSessionAsync(client);
        var messagesBefore =
            await factory.CountContactMessagesAsync();

        var contact = CreateValidRequest();
        contact.Website = "https://spam.example.com";

        using var request = CreatePostRequest(
            "/api/contact",
            session.AntiforgeryToken,
            contact);

        var response = await client.SendAsync(request);
        var messagesAfter =
            await factory.CountContactMessagesAsync();

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        Assert.Equal(messagesBefore, messagesAfter);
    }

    [Fact]
    public async Task GetMessages_WhenAnonymous_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/contact");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        var client = CreateClient();

        await AuthenticateAsync(
            client,
            "contact-user@skillsnap.test",
            role: "User");

        var response = await client.GetAsync("/api/contact");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_WhenUserIsAdmin_ReturnsMessages()
    {
        var client = CreateClient();
        var session = await GetSessionAsync(client);

        using var submitRequest = CreatePostRequest(
            "/api/contact",
            session.AntiforgeryToken,
            new CreateContactMessageRequest
            {
                Name = "Hiring Manager",
                Email = "hiring@example.com",
                Message =
                    "I would like to discuss your portfolio."
            });

        var submitResponse =
            await client.SendAsync(submitRequest);

        Assert.Equal(
            HttpStatusCode.Accepted,
            submitResponse.StatusCode);

        await AuthenticateAsync(
            client,
            "contact-admin@skillsnap.test",
            role: "Admin");

        var messages = await client
            .GetFromJsonAsync<List<ContactMessageResponse>>(
                "/api/contact");

        Assert.NotNull(messages);

        Assert.Contains(
            messages,
            message =>
                message.Email == "hiring@example.com" &&
                message.Name == "Hiring Manager");
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    private static async Task<SessionResponse> GetSessionAsync(
        HttpClient client)
    {
        var session = await client
            .GetFromJsonAsync<SessionResponse>(
                "/api/auth/session");

        return Assert.IsType<SessionResponse>(session);
    }

    private async Task AuthenticateAsync(
        HttpClient client,
        string email,
        string role)
    {
        const string password = "TestPassword123!";

        await factory.CreateUserAsync(
            email,
            password,
            role);

        var session = await GetSessionAsync(client);

        using var loginRequest = CreatePostRequest(
            "/api/auth/login",
            session.AntiforgeryToken,
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        var response = await client.SendAsync(loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpRequestMessage CreatePostRequest<T>(
        string uri,
        string antiforgeryToken,
        T content)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            uri);

        request.Headers.Add(
            "X-CSRF-TOKEN",
            antiforgeryToken);

        request.Content = JsonContent.Create(content);

        return request;
    }

    private static CreateContactMessageRequest CreateValidRequest()
    {
        return new CreateContactMessageRequest
        {
            Name = "Recruiter Example",
            Email = "recruiter@example.com",
            Message =
                "I would like to discuss a .NET opportunity."
        };
    }
}
