using System.Net.Http.Json;
using SkillSnap.Client.Models;
using SkillSnap.Contracts.Skills;

namespace SkillSnap.Client.Services;

public class SkillService
{
    private readonly HttpClient httpClient;

    public SkillService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<Skill>> GetSkillsAsync()
    {
        var skills = await httpClient
            .GetFromJsonAsync<List<SkillResponse>>(
                "api/skills");

        return skills?.Select(ToModel).ToList() ?? [];
    }

    public async Task<Skill?> AddSkillAsync(
        Skill newSkill)
    {
        var response =
            await httpClient.PostAsJsonAsync(
                "api/skills",
                ToRequest(newSkill));

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<SkillResponse>();

        return result is null ? null : ToModel(result);
    }

    public async Task UpdateSkillAsync(Skill skill)
    {
        var response =
            await httpClient.PutAsJsonAsync(
                $"api/skills/{skill.Id}",
                ToRequest(skill));

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteSkillAsync(int skillId)
    {
        var response =
            await httpClient.DeleteAsync(
                $"api/skills/{skillId}");

        response.EnsureSuccessStatusCode();
    }

    private static SaveSkillRequest ToRequest(Skill skill) => new()
    {
        Name = skill.Name
    };

    private static Skill ToModel(SkillResponse skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name
    };
}
