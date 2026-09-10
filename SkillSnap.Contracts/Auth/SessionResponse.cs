namespace SkillSnap.Contracts.Auth;

public sealed record SessionResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Email,
    IReadOnlyList<string> Roles,
    string AntiforgeryToken);
