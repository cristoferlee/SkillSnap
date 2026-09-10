using System.ComponentModel.DataAnnotations;
using SkillSnap.Contracts.Contact;

namespace SkillSnap.Api.Tests;

public sealed class ContactMessageValidationTests
{
    [Fact]
    public void ValidContactMessage_PassesValidation()
    {
        var request = new CreateContactMessageRequest
        {
            Name = "José O'Connor-Lee",
            Email = "jose@example.com",
            Message = "I would like to discuss a .NET opportunity."
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Name_RejectsMissingShortAndNumericValues()
    {
        Assert.Contains(
            Validate(CreateRequest(name: "")),
            error => error.MemberNames.Contains("Name"));

        Assert.Contains(
            Validate(CreateRequest(name: "J")),
            error => error.MemberNames.Contains("Name"));

        Assert.Contains(
            Validate(CreateRequest(name: "John 123")),
            error => error.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Email_RejectsInvalidAddress()
    {
        var errors = Validate(
            CreateRequest(email: "not-an-email"));

        Assert.Contains(
            errors,
            error => error.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Message_RejectsShortAndOversizedValues()
    {
        Assert.Contains(
            Validate(CreateRequest(message: "Too short")),
            error => error.MemberNames.Contains("Message"));

        Assert.Contains(
            Validate(CreateRequest(
                message: new string('a', 2001))),
            error => error.MemberNames.Contains("Message"));
    }

    private static CreateContactMessageRequest CreateRequest(
        string name = "Christoper Chaves Lee",
        string email = "contact@example.com",
        string message = "This is a valid contact message.")
    {
        return new CreateContactMessageRequest
        {
            Name = name,
            Email = email,
            Message = message
        };
    }

    private static List<ValidationResult> Validate(
        CreateContactMessageRequest request)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        return results;
    }
}
