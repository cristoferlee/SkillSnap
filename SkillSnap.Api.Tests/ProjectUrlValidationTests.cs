using System.ComponentModel.DataAnnotations;
using SkillSnap.Contracts.Projects;
using SkillSnap.Contracts.Validation;

namespace SkillSnap.Api.Tests;

public sealed class ProjectUrlValidationTests
{
    [Theory]
    [InlineData("/images/projects/skillsnap.svg")]
    [InlineData("/images/projects/revestik-cover.webp")]
    [InlineData("https://example.com/images/project.png")]
    public void ProjectImageUrl_AcceptsLocalProjectImagesAndHttpsUrls(
        string value)
    {
        var validator = new ProjectImageUrlAttribute();

        Assert.True(validator.IsValid(value));
    }

    [Theory]
    [InlineData("http://example.com/image.png")]
    [InlineData("/images/avatar.png")]
    [InlineData("/images/projects/")]
    [InlineData("/images/projects/../secret.txt")]
    [InlineData("/images/projects/folder/image.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user@example.com/image.png")]
    public void ProjectImageUrl_RejectsInvalidOrDisallowedLocations(
        string value)
    {
        var validator = new ProjectImageUrlAttribute();

        Assert.False(validator.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/projects/demo?view=public#overview")]
    public void OptionalHttpsUrl_AcceptsEmptyValuesAndHttpsUrls(string? value)
    {
        var validator = new OptionalHttpsUrlAttribute();

        Assert.True(validator.IsValid(value));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("/projects/demo")]
    [InlineData("example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://")]
    [InlineData("https://user@example.com")]
    [InlineData("https://user:password@example.com")]
    public void OptionalHttpsUrl_RejectsInvalidOrDisallowedUrls(string value)
    {
        var validator = new OptionalHttpsUrlAttribute();

        Assert.False(validator.IsValid(value));
    }

    [Theory]
    [InlineData(nameof(SaveProjectRequest.LiveUrl), 2048, true)]
    [InlineData(nameof(SaveProjectRequest.LiveUrl), 2049, false)]
    [InlineData(nameof(SaveProjectRequest.RepositoryUrl), 2048, true)]
    [InlineData(nameof(SaveProjectRequest.RepositoryUrl), 2049, false)]
    public void ProjectUrl_EnforcesContractLengthLimit(
        string propertyName,
        int length,
        bool expectedValidity)
    {
        const string prefix = "https://example.com/";
        var url = prefix + new string('a', length - prefix.Length);
        var context = new ValidationContext(new SaveProjectRequest())
        {
            MemberName = propertyName
        };
        var errors = new List<ValidationResult>();

        var isValid = Validator.TryValidateProperty(url, context, errors);

        Assert.Equal(expectedValidity, isValid);
        if (expectedValidity)
        {
            Assert.Empty(errors);
        }
        else
        {
            Assert.Contains(errors, error => error.MemberNames.Contains(propertyName));
        }
    }
}
