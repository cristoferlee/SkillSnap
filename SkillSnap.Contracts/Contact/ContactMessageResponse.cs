namespace SkillSnap.Contracts.Contact;

public sealed record ContactMessageResponse(
    int Id,
    string Name,
    string Email,
    string Message,
    DateTime CreatedAtUtc);
