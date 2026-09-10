using System.Net.Http.Json;
using SkillSnap.Client.Models;
using SkillSnap.Contracts.Projects;

namespace SkillSnap.Client.Services;

public class ProjectService
{
    private readonly HttpClient httpClient;

    public ProjectService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        var projects = await httpClient
            .GetFromJsonAsync<List<ProjectResponse>>(
                "api/projects");

        return projects?.Select(ToModel).ToList() ?? [];
    }

    public async Task<Project?> AddProjectAsync(
        Project newProject)
    {
        var response =
            await httpClient.PostAsJsonAsync(
                "api/projects",
                ToRequest(newProject));

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ProjectResponse>();

        return result is null ? null : ToModel(result);
    }

    public async Task UpdateProjectAsync(
        Project project)
    {
        var response =
            await httpClient.PutAsJsonAsync(
                $"api/projects/{project.Id}",
                ToRequest(project));

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        var response =
            await httpClient.DeleteAsync(
                $"api/projects/{projectId}");

        response.EnsureSuccessStatusCode();
    }

    private static SaveProjectRequest ToRequest(Project project) => new()
    {
        Title = project.Title,
        Description = project.Description,
        ImageUrl = project.ImageUrl,
        LiveUrl = project.LiveUrl,
        RepositoryUrl = project.RepositoryUrl,
        SkillIds = project.SkillIds
    };

    private static Project ToModel(ProjectResponse project) => new()
    {
        Id = project.Id,
        Title = project.Title,
        Description = project.Description,
        ImageUrl = project.ImageUrl,
        LiveUrl = project.LiveUrl,
        RepositoryUrl = project.RepositoryUrl,
        Skills = project.Skills
            .Select(skill => new Skill
            {
                Id = skill.Id,
                Name = skill.Name
            })
            .ToList(),
        SkillIds = project.Skills
            .Select(skill => skill.Id)
            .ToList()
    };
}
