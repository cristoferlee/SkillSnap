using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Contracts.Contact;

public sealed class CreateContactMessageRequest
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Name must be between 2 and 100 characters.")]
    [RegularExpression(
        @"^[\p{L}\p{M}\s'.-]+$",
        ErrorMessage =
            "Name cannot contain numbers or unsupported characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(
        ErrorMessage = "Please enter a valid email address.")]
    [StringLength(
        254,
        ErrorMessage = "Email cannot exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write a message.")]
    [StringLength(
        2000,
        MinimumLength = 10,
        ErrorMessage =
            "Message must be between 10 and 2000 characters.")]
    public string Message { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Website { get; set; }
}
