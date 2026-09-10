using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillSnap.Api.Models;
using SkillSnap.Contracts.Auth;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IAntiforgery antiforgery;

    public AuthController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.antiforgery = antiforgery;
    }

    [AllowAnonymous]
    [HttpGet("session")]
    public async Task<ActionResult<SessionResponse>> GetSession() =>
        Ok(await CreateSessionResponseAsync());

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<SessionResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        HttpContext.User = await signInManager.CreateUserPrincipalAsync(user);
        return Ok(await CreateSessionResponseAsync());
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<SessionResponse>> Logout()
    {
        await signInManager.SignOutAsync();
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        return Ok(await CreateSessionResponseAsync());
    }

    private async Task<SessionResponse> CreateSessionResponseAsync()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        var authenticated = User.Identity?.IsAuthenticated == true;
        var user = authenticated ? await userManager.GetUserAsync(User) : null;
        var roles = user is null ? [] : await userManager.GetRolesAsync(user);

        return new SessionResponse(
            authenticated,
            user?.Id,
            user?.Email,
            roles.ToArray(),
            tokens.RequestToken ?? throw new InvalidOperationException(
                "An antiforgery request token could not be generated."));
    }
}
