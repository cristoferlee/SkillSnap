namespace SkillSnap.Client.Services;

public class UserSessionService
{
    public string? UserId { get; private set; }

    public string? Email { get; private set; }

    public string? Role { get; private set; }

    public string? CurrentProjectTitle { get; private set; }

    public event Action? StateChanged;

    public void SetUser(
        string? userId,
        string? email,
        string? role)
    {
        UserId = userId;
        Email = email;
        Role = role;

        StateChanged?.Invoke();
    }

    public void SetCurrentProject(string? projectTitle)
    {
        CurrentProjectTitle = projectTitle;

        StateChanged?.Invoke();
    }

    public void Clear()
    {
        UserId = null;
        Email = null;
        Role = null;
        CurrentProjectTitle = null;

        StateChanged?.Invoke();
    }
}