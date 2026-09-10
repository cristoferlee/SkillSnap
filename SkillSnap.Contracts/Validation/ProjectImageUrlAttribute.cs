using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Contracts.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ProjectImageUrlAttribute : ValidationAttribute
{
    private const string LocalImagePrefix = "/images/projects/";

    public ProjectImageUrlAttribute()
        : base("{0} must be an HTTPS URL or a local project image path.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.StartsWith(LocalImagePrefix, StringComparison.Ordinal))
        {
            var fileName = text[LocalImagePrefix.Length..];

            return fileName.Length > 0
                && !fileName.Contains("..", StringComparison.Ordinal)
                && fileName.All(character =>
                    char.IsLetterOrDigit(character) ||
                    character is '-' or '_' or '.');
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrWhiteSpace(uri.Host)
            && string.IsNullOrEmpty(uri.UserInfo);
    }
}
