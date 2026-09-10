using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkillSnap.Api.Models;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? LiveUrl { get; set; }

    [MaxLength(2048)]
    public string? RepositoryUrl { get; set; }

    public int PortfolioUserId { get; set; }

    [ForeignKey(nameof(PortfolioUserId))]
    [JsonIgnore]
    public PortfolioUser? PortfolioUser { get; set; }

    public ICollection<ProjectSkill> ProjectSkills { get; set; }
        = [];
}
