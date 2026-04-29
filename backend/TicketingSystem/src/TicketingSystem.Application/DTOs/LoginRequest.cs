using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Application.DTOs;

public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);
