using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Client.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(
        254,
        ErrorMessage = "Email cannot exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
        128,
        ErrorMessage = "Password cannot exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;
}