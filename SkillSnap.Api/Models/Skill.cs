using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkillSnap.Api.Models;

public class Skill
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int PortfolioUserId { get; set; }

    [ForeignKey(nameof(PortfolioUserId))]
    [JsonIgnore]
    public PortfolioUser? PortfolioUser { get; set; }

    public ICollection<ProjectSkill> ProjectSkills { get; set; }
        = [];
}
