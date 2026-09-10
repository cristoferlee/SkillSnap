using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;
using SkillSnap.Contracts.Skills;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillSnapContext context;
    private readonly ILogger<SkillsController> logger;

    public SkillsController(
        SkillSnapContext context,
        ILogger<SkillsController> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SkillResponse>>> GetSkills()
    {
        var stopwatch = Stopwatch.StartNew();

        var skills = await context.Skills
            .AsNoTracking()
            .ToListAsync();

        stopwatch.Stop();

        logger.LogInformation(
            "GET /api/skills completed in {ElapsedMilliseconds} ms.",
            stopwatch.ElapsedMilliseconds);

        return Ok(skills.Select(ToResponse));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<SkillResponse>> AddSkill(
        SaveSkillRequest request)
    {
        var ownerId = await context.PortfolioUsers.AsNoTracking()
            .Select(user => (int?)user.Id)
            .FirstOrDefaultAsync();

        if (ownerId is null)
        {
            return Conflict(new { message = "Portfolio data must be initialized first." });
        }

        var newSkill = new Skill
        {
            Name = request.Name.Trim(),
            PortfolioUserId = ownerId.Value
        };

        context.Skills.Add(newSkill);
        await context.SaveChangesAsync();

        return Created(
            $"/api/skills/{newSkill.Id}",
            ToResponse(newSkill));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSkill(
        int id,
        SaveSkillRequest request)
    {
        var existingSkill =
            await context.Skills.FindAsync(id);

        if (existingSkill is null)
        {
            return NotFound();
        }

        existingSkill.Name = request.Name.Trim();

        await context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var skill =
            await context.Skills.FindAsync(id);

        if (skill is null)
        {
            return NotFound();
        }

        context.Skills.Remove(skill);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static SkillResponse ToResponse(Skill skill) => new(
        skill.Id,
        skill.Name);
}
