using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;

namespace SkillSnap.Api.Tests;

public sealed class SkillSnapApiFactory :
    WebApplicationFactory<Program>,
    IAsyncLifetime
{
    private readonly SqliteConnection connection =
        new("Data Source=:memory:");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Admin:Email"] = string.Empty,
                    ["Admin:Password"] = string.Empty
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<SkillSnapContext>>();

            services.RemoveAll<SkillSnapContext>();

            services.AddDbContext<SkillSnapContext>(
                options => options.UseSqlite(connection));
        });
    }

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();

        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<SkillSnapContext>();

        await context.Database.EnsureCreatedAsync();
    }

    public async Task SeedPortfolioAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<SkillSnapContext>();

        if (await context.PortfolioUsers.AnyAsync())
        {
            return;
        }

        context.PortfolioUsers.Add(
            new PortfolioUser
            {
                Name = "Christoper Chaves Lee",
                Bio = "Test portfolio biography.",
                ProfileImageUrl =
                    "https://example.com/profile.png",
                Projects =
                [
                    new Project
                    {
                        Title = "Test project",
                        Description =
                            "A project created for integration testing.",
                        ImageUrl =
                            "https://example.com/project.png"
                    }
                ],
                Skills =
                [
                    new Skill
                    {
                        Name = "C#"
                    }
                ]
            });

        await context.SaveChangesAsync();
    }

    public async Task CreateUserAsync(
        string email,
        string password,
        string? role = null)
    {
        using var scope = Services.CreateScope();

        var users = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roles = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var user = await users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email
            };

            var result = await users.CreateAsync(
                user,
                password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "The test user could not be created.");
            }
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        if (!await roles.RoleExistsAsync(role))
        {
            var result = await roles.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "The test role could not be created.");
            }
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            var result = await users.AddToRoleAsync(
                user,
                role);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "The test role could not be assigned.");
            }
        }
    }

    public async Task<int> CountContactMessagesAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<SkillSnapContext>();

        return await context.ContactMessages.CountAsync();
    }

    public new async Task DisposeAsync()
    {
        Dispose();
        await connection.DisposeAsync();
    }
}
