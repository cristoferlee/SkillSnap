using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Api.Models;
using SkillSnap.Contracts.Contact;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/contact")]
public sealed class ContactController : ControllerBase
{
    private readonly SkillSnapContext context;

    public ContactController(SkillSnapContext context)
    {
        this.context = context;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<
        ActionResult<IEnumerable<ContactMessageResponse>>> GetMessages()
    {
        var messages = await context.ContactMessages
            .AsNoTracking()
            .OrderByDescending(contact => contact.CreatedAtUtc)
            .Select(contact => new ContactMessageResponse(
                contact.Id,
                contact.Name,
                contact.Email,
                contact.Message,
                contact.CreatedAtUtc))
            .ToListAsync();

        return Ok(messages);
    }

    [EnableRateLimiting("contact")]
    [HttpPost]
    public async Task<IActionResult> Submit(
        CreateContactMessageRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Accepted();
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var message = request.Message.Trim();

        if (name.Length < 2)
        {
            ModelState.AddModelError(
                nameof(request.Name),
                "Name must be at least 2 characters.");
        }

        if (message.Length < 10)
        {
            ModelState.AddModelError(
                nameof(request.Message),
                "Message must be at least 10 characters.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var contactMessage = new ContactMessage
        {
            Name = name,
            Email = email,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.ContactMessages.Add(contactMessage);
        await context.SaveChangesAsync();

        return Accepted();
    }
}
