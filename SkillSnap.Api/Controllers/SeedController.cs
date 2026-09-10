using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly SkillSnapContext context;

    public SeedController(SkillSnapContext context)
    {
        this.context = context;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        if (await context.PortfolioUsers.AnyAsync())
        {
            return BadRequest(
                "Sample data already exists.");
        }

        var user = new PortfolioUser
        {
            Name = "Christoper Chaves Lee",

            Bio =
                ".NET full-stack developer building secure business applications with C#, ASP.NET Core, and Blazor.",

            ProfileImageUrl =
                "https://placehold.co/300x300?text=CCL",

            Projects =
            [
                new Project
                {
                    Title = "Task Tracker",

                    Description =
                        "Manage tasks effectively",

                    ImageUrl =
                        "https://placehold.co/600x200" +
                        "?text=Task+Tracker"
                },

                new Project
                {
                    Title = "Weather App",

                    Description =
                        "Forecast weather using APIs",

                    ImageUrl =
                        "https://placehold.co/600x200" +
                        "?text=Weather+App"
                }
            ],

            Skills =
            [
                new Skill
                {
                    Name = "C#"
                },

                new Skill
                {
                    Name = "Blazor"
                }
            ]
        };

        context.PortfolioUsers.Add(user);
        await context.SaveChangesAsync();

        return Ok("Sample data inserted.");
    }
}
