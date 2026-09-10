using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Contracts.Skills;

public sealed class SaveSkillRequest
{
    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;
}
