using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillSnap.Contracts.Projects;
using SkillSnap.Contracts.Skills;

namespace SkillSnap.Api.Tests;

public sealed class PublicEndpointsTests :
    IClassFixture<SkillSnapApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly SkillSnapApiFactory factory;

    public PublicEndpointsTests(SkillSnapApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetProjects_ReturnsPublicContractWithoutOwnerId()
    {
        // Arrange
        await factory.SeedPortfolioAsync();
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/projects");
        var json = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<List<ProjectResponse>>(
            json,
            JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            "portfolioUserId",
            json,
            StringComparison.OrdinalIgnoreCase);

        var project = Assert.Single(Assert.IsType<List<ProjectResponse>>(projects));
        Assert.Equal("Test project", project.Title);
        Assert.Equal(
            "A project created for integration testing.",
            project.Description);
        Assert.Equal(
            "https://example.com/project.png",
            project.ImageUrl);
    }

    [Fact]
    public async Task GetSkills_ReturnsPublicContractWithoutOwnerId()
    {
        // Arrange
        await factory.SeedPortfolioAsync();
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/skills");
        var json = await response.Content.ReadAsStringAsync();
        var skills = JsonSerializer.Deserialize<List<SkillResponse>>(
            json,
            JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            "portfolioUserId",
            json,
            StringComparison.OrdinalIgnoreCase);

        var skill = Assert.Single(Assert.IsType<List<SkillResponse>>(skills));
        Assert.Equal("C#", skill.Name);
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
}
