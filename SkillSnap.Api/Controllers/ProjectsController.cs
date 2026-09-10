using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;
using SkillSnap.Contracts.Projects;
using SkillSnap.Contracts.Skills;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly SkillSnapContext context;
    private readonly ILogger<ProjectsController> logger;

    public ProjectsController(
        SkillSnapContext context,
        ILogger<ProjectsController> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects()
    {
        var stopwatch = Stopwatch.StartNew();

        var projects = await context.Projects
            .AsNoTracking()
            .Include(project => project.ProjectSkills)
                .ThenInclude(projectSkill => projectSkill.Skill)
            .ToListAsync();

        stopwatch.Stop();

        logger.LogInformation(
            "GET /api/projects completed in {ElapsedMilliseconds} ms.",
            stopwatch.ElapsedMilliseconds);

        return Ok(projects.Select(ToResponse));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> AddProject(
        SaveProjectRequest request)
    {
        var ownerId = await context.PortfolioUsers.AsNoTracking()
            .Select(user => (int?)user.Id)
            .FirstOrDefaultAsync();

        if (ownerId is null)
        {
            return Conflict(new { message = "Portfolio data must be initialized first." });
        }

        var requestedSkills = await LoadRequestedSkillsAsync(
            request.SkillIds,
            ownerId.Value);

        if (requestedSkills is null)
        {
            return BadRequest(new
            {
                message = "One or more selected skills are invalid."
            });
        }

        var newProject = new Project
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            LiveUrl = NormalizeOptionalUrl(request.LiveUrl),
            RepositoryUrl = NormalizeOptionalUrl(request.RepositoryUrl),
            PortfolioUserId = ownerId.Value,
            ProjectSkills = requestedSkills
                .Select(skill => new ProjectSkill
                {
                    SkillId = skill.Id,
                    Skill = skill
                })
                .ToList()
        };

        context.Projects.Add(newProject);
        await context.SaveChangesAsync();

        return Created(
            $"/api/projects/{newProject.Id}",
            ToResponse(newProject));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProject(
        int id,
        SaveProjectRequest request)
    {
        var existingProject = await context.Projects
            .Include(project => project.ProjectSkills)
            .SingleOrDefaultAsync(project => project.Id == id);

        if (existingProject is null)
        {
            return NotFound();
        }

        var requestedSkills = await LoadRequestedSkillsAsync(
            request.SkillIds,
            existingProject.PortfolioUserId);

        if (requestedSkills is null)
        {
            return BadRequest(new
            {
                message = "One or more selected skills are invalid."
            });
        }

        var requestedSkillIds = requestedSkills
            .Select(skill => skill.Id)
            .ToHashSet();

        var removedProjectSkills = existingProject.ProjectSkills
            .Where(projectSkill =>
                !requestedSkillIds.Contains(projectSkill.SkillId))
            .ToList();

        context.RemoveRange(removedProjectSkills);

        var existingSkillIds = existingProject.ProjectSkills
            .Select(projectSkill => projectSkill.SkillId)
            .ToHashSet();

        foreach (var skill in requestedSkills)
        {
            if (!existingSkillIds.Contains(skill.Id))
            {
                existingProject.ProjectSkills.Add(new ProjectSkill
                {
                    ProjectId = existingProject.Id,
                    SkillId = skill.Id,
                    Skill = skill
                });
            }
        }

        existingProject.Title = request.Title.Trim();
        existingProject.Description = request.Description.Trim();
        existingProject.ImageUrl = request.ImageUrl.Trim();
        existingProject.LiveUrl =
            NormalizeOptionalUrl(request.LiveUrl);
        existingProject.RepositoryUrl =
            NormalizeOptionalUrl(request.RepositoryUrl);

        await context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project =
            await context.Projects.FindAsync(id);

        if (project is null)
        {
            return NotFound();
        }

        context.Projects.Remove(project);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static ProjectResponse ToResponse(Project project) => new(
        project.Id,
        project.Title,
        project.Description,
        project.ImageUrl,
        project.LiveUrl,
        project.RepositoryUrl)
    {
        Skills = project.ProjectSkills
            .OrderBy(projectSkill => projectSkill.Skill.Name)
            .Select(projectSkill => new SkillResponse(
                projectSkill.Skill.Id,
                projectSkill.Skill.Name))
            .ToList()
    };

    private static string? NormalizeOptionalUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private async Task<List<Skill>?> LoadRequestedSkillsAsync(
        IEnumerable<int> requestedSkillIds,
        int portfolioUserId)
    {
        var skillIds = requestedSkillIds
            .Distinct()
            .ToList();

        var skills = await context.Skills
            .Where(skill =>
                skill.PortfolioUserId == portfolioUserId &&
                skillIds.Contains(skill.Id))
            .ToListAsync();

        return skills.Count == skillIds.Count
            ? skills
            : null;
    }
}
