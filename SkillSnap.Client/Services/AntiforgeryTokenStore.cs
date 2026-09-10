namespace SkillSnap.Client.Services;

public sealed class AntiforgeryTokenStore
{
    public string? Token { get; private set; }

    public void Set(string token)
    {
        Token = token;
    }

    public void Clear()
    {
        Token = null;
    }
}