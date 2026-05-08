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

    public AuthController(ILoginHandler loginHandler)
    {
        _loginHandler = loginHandler;
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
}
