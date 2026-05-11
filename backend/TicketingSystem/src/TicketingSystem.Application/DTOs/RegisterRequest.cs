using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Application.DTOs;

public record RegisterRequest(
    [Required] string Name,
    [Required] [EmailAddress] string Email,
    [Required] string Password
);