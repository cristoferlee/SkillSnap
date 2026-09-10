using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSnap.Api.Models;

public class ProjectSkill
{
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    public int SkillId { get; set; }

    [ForeignKey(nameof(SkillId))]
    public Skill Skill { get; set; } = null!;
}
