using System.ComponentModel.DataAnnotations;
using SkillSnap.Contracts.Validation;

namespace SkillSnap.Client.Models;

public class Project
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Image URL is required.")]
    [ProjectImageUrl]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(2048)]
    [OptionalHttpsUrl]
    public string? LiveUrl { get; set; }

    [StringLength(2048)]
    [OptionalHttpsUrl]
    public string? RepositoryUrl { get; set; }

    public List<Skill> Skills { get; set; } = [];

    public List<int> SkillIds { get; set; } = [];
}
