using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillSnap.Contracts.Auth;
using SkillSnap.Contracts.Projects;
using SkillSnap.Contracts.Skills;

namespace SkillSnap.Api.Tests;

public sealed class AuthorizationTests :
    IClassFixture<SkillSnapApiFactory>
{
    private readonly SkillSnapApiFactory factory;

    public AuthorizationTests(SkillSnapApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateProject_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // Arrange
        const string email = "user@skillsnap.test";
        const string password = "TestPassword123!";

        await factory.SeedPortfolioAsync();
        await factory.CreateUserAsync(email, password, role: "User");

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var anonymousSession = await client
            .GetFromJsonAsync<SessionResponse>("/api/auth/session");

        Assert.NotNull(anonymousSession);

        using var loginRequest = CreatePostRequest(
            "/api/auth/login",
            anonymousSession.AntiforgeryToken,
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        var loginResponse = await client.SendAsync(loginRequest);
        var userSession = await loginResponse.Content
            .ReadFromJsonAsync<SessionResponse>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(userSession);

        using var createRequest = CreatePostRequest(
            "/api/projects",
            userSession.AntiforgeryToken,
            new SaveProjectRequest
            {
                Title = "Forbidden project",
                Description =
                    "This project must not be created by a regular user.",
                ImageUrl = "https://example.com/forbidden.png"
            });

        // Act
        var response = await client.SendAsync(createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WhenUserIsAdmin_ReturnsCreated()
    {
        // Arrange
        const string email = "admin-write@skillsnap.test";
        const string password = "TestPassword123!";

        await factory.SeedPortfolioAsync();
        await factory.CreateUserAsync(email, password, role: "Admin");

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var skills = await client
            .GetFromJsonAsync<List<SkillResponse>>("/api/skills");

        Assert.NotNull(skills);

        var selectedSkill = Assert.Single(skills);

        var anonymousSession = await client
            .GetFromJsonAsync<SessionResponse>("/api/auth/session");

        Assert.NotNull(anonymousSession);

        using var loginRequest = CreatePostRequest(
            "/api/auth/login",
            anonymousSession.AntiforgeryToken,
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        var loginResponse = await client.SendAsync(loginRequest);
        var adminSession = await loginResponse.Content
            .ReadFromJsonAsync<SessionResponse>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(adminSession);

        using var createRequest = CreatePostRequest(
            "/api/projects",
            adminSession.AntiforgeryToken,
            new SaveProjectRequest
            {
                Title = "Admin project",
                Description =
                    "A project created by an authorized administrator.",
                ImageUrl = "https://example.com/admin-project.png",
                LiveUrl = "  https://revestik.example.com  ",
                RepositoryUrl =
                    "  https://github.com/cristoferlee/revestik  ",
                SkillIds = [selectedSkill.Id]
            });

        // Act
        var response = await client.SendAsync(createRequest);
        var project = await response.Content
            .ReadFromJsonAsync<ProjectResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(project);
        Assert.True(project.Id > 0);
        Assert.Equal("Admin project", project.Title);
        Assert.Equal(
            "https://revestik.example.com",
            project.LiveUrl);
        Assert.Equal(
            "https://github.com/cristoferlee/revestik",
            project.RepositoryUrl);

        var projectSkill = Assert.Single(project.Skills);

        Assert.Equal(selectedSkill.Id, projectSkill.Id);
        Assert.Equal("C#", projectSkill.Name);

        var projects = await client
            .GetFromJsonAsync<List<ProjectResponse>>("/api/projects");

        Assert.NotNull(projects);

        var savedProject = Assert.Single(
            projects,
            item => item.Id == project.Id);

        Assert.Equal(project.LiveUrl, savedProject.LiveUrl);
        Assert.Equal(
            project.RepositoryUrl,
            savedProject.RepositoryUrl);

        var savedSkill = Assert.Single(savedProject.Skills);

        Assert.Equal(projectSkill, savedSkill);

        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/projects/{project.Id}");

        updateRequest.Headers.Add(
            "X-CSRF-TOKEN",
            adminSession.AntiforgeryToken);

        updateRequest.Content = JsonContent.Create(
            new SaveProjectRequest
            {
                Title = project.Title,
                Description = project.Description,
                ImageUrl = project.ImageUrl,
                LiveUrl = project.LiveUrl,
                RepositoryUrl = project.RepositoryUrl,
                SkillIds = []
            });

        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        var updatedProjects = await client
            .GetFromJsonAsync<List<ProjectResponse>>("/api/projects");

        Assert.NotNull(updatedProjects);

        var updatedProject = Assert.Single(
            updatedProjects,
            item => item.Id == project.Id);

        Assert.Empty(updatedProject.Skills);
    }

    [Fact]
    public async Task CreateProject_WithInsecureLiveUrl_ReturnsBadRequestAndDoesNotSave()
    {
        // Arrange
        const string email = "validation-admin@skillsnap.test";
        const string password = "TestPassword123!";

        await factory.SeedPortfolioAsync();
        await factory.CreateUserAsync(email, password, role: "Admin");

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        var projectsBefore = await client
            .GetFromJsonAsync<List<ProjectResponse>>("/api/projects");

        Assert.NotNull(projectsBefore);

        var anonymousSession = await client
            .GetFromJsonAsync<SessionResponse>("/api/auth/session");

        Assert.NotNull(anonymousSession);

        using var loginRequest = CreatePostRequest(
            "/api/auth/login",
            anonymousSession.AntiforgeryToken,
            new LoginRequest
            {
                Email = email,
                Password = password
            });

        var loginResponse = await client.SendAsync(loginRequest);
        var adminSession = await loginResponse.Content
            .ReadFromJsonAsync<SessionResponse>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(adminSession);

        using var createRequest = CreatePostRequest(
            "/api/projects",
            adminSession.AntiforgeryToken,
            new SaveProjectRequest
            {
                Title = "Invalid URL project",
                Description =
                    "This otherwise valid project uses an insecure live URL.",
                ImageUrl = "https://example.com/project.png",
                LiveUrl = "http://example.com"
            });

        // Act
        var response = await client.SendAsync(createRequest);
        var projectsAfter = await client
            .GetFromJsonAsync<List<ProjectResponse>>("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(projectsAfter);
        Assert.Equal(projectsBefore.Count, projectsAfter.Count);
    }

    private static HttpRequestMessage CreatePostRequest<T>(
        string uri,
        string antiforgeryToken,
        T content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        request.Content = JsonContent.Create(content);
        return request;
    }
}
