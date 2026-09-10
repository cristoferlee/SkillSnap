using System.ComponentModel.DataAnnotations;
using SkillSnap.Contracts.Validation;

namespace SkillSnap.Contracts.Projects;

public sealed class SaveProjectRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required, ProjectImageUrl, StringLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(2048), OptionalHttpsUrl]
    public string? LiveUrl { get; set; }

    [StringLength(2048), OptionalHttpsUrl]
    public string? RepositoryUrl { get; set; }

    public List<int> SkillIds { get; set; } = [];
}
