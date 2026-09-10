using SkillSnap.Contracts.Skills;

namespace SkillSnap.Contracts.Projects;

public sealed record ProjectResponse(
    int Id,
    string Title,
    string Description,
    string ImageUrl,
    string? LiveUrl = null,
    string? RepositoryUrl = null)
{
    public IReadOnlyList<SkillResponse> Skills { get; init; }
        = [];
}
