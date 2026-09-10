using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Api.Models;

public sealed class ContactMessage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
