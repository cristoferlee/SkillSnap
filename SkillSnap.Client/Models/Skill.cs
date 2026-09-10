using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Client.Models;

public class Skill
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; } = string.Empty;
}
