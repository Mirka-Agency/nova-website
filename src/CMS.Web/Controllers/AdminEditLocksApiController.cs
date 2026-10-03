using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CMS.Application.Editing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/edit-locks")]
public sealed class AdminEditLocksApiController : ControllerBase
{
    private readonly IEditLockService _editLocks;

    public AdminEditLocksApiController(IEditLockService editLocks)
    {
        _editLocks = editLocks;
    }

    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Heartbeat(
        [FromBody] EditLockMutationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var ok = await _editLocks.HeartbeatAsync(
            request.EntityType,
            request.EntityId,
            userId,
            request.LockToken,
            cancellationToken);

        return ok ? NoContent() : Conflict();
    }

    [HttpPost("release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Release(
        [FromBody] EditLockMutationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        await _editLocks.ReleaseAsync(
            request.EntityType,
            request.EntityId,
            userId,
            request.LockToken,
            cancellationToken);

        return NoContent();
    }

    private bool TryGetUserId(out string userId)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(userId);
    }
}

public sealed class EditLockMutationRequest
{
    [Required]
    [MaxLength(64)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string LockToken { get; set; } = string.Empty;
}
