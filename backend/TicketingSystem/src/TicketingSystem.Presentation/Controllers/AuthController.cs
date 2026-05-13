using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;

namespace TicketingSystem.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILoginHandler _loginHandler;
    private readonly IRegisterHandler _registerHandler;

    public AuthController(ILoginHandler loginHandler, IRegisterHandler registerHandler)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
    }

    [HttpPost("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _loginHandler.HandleAsync(request, cancellationToken);

        if (response == null)
        {
            return Unauthorized(new { Error = "INVALID_CREDENTIALS", Message = "Email o contraseña incorrectos." });
        }

        return Ok(response);
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _registerHandler.HandleAsync(request, cancellationToken);

        if (response == null)
        {
            return BadRequest(new { Error = "EMAIL_IN_USE", Message = "El email ya está en uso." });
        }

        return Ok(response);
    }
}