using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMusic.API.Auth;
using MyMusic.API.Contracts;
using MyMusic.Core.Services;

namespace MyMusic.API.Controllers;

/// <summary>
/// Registration, sign-in, and the signed-in user's own account. There is no
/// endpoint to list or edit other users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService users, TokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await users.RegisterAsync(request.Username, request.Password, request.FirstName, request.LastName, cancellationToken);
        if (result.Status != ServiceStatus.Success)
        {
            return this.ToProblem(result);
        }

        var (token, expiresAt) = tokens.CreateToken(result.Value!);
        return CreatedAtAction(nameof(Me), null, new TokenResponse(token, expiresAt, UserResponse.From(result.Value!)));
    }

    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<ActionResult<TokenResponse>> Authenticate(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.AuthenticateAsync(request.Username, request.Password, cancellationToken);
        if (user is null)
        {
            return Problem("Username or password is incorrect.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = tokens.CreateToken(user);
        return new TokenResponse(token, expiresAt, UserResponse.From(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(User.UserId(), cancellationToken);
        return user is null ? NotFound() : UserResponse.From(user);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.UpdateAsync(User.UserId(), request.FirstName, request.LastName, request.Password, cancellationToken);
        return user is null ? NotFound() : UserResponse.From(user);
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken) =>
        await users.DeleteAsync(User.UserId(), cancellationToken) ? NoContent() : NotFound();
}
