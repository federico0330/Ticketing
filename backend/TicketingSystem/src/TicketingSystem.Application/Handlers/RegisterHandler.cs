using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Security;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Handlers;

public class RegisterHandler : IRegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse?> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            return null;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password)
        };

        await _userRepository.CreateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var role = _jwtTokenService.ResolveRole(user.Email);
        var token = _jwtTokenService.GenerateToken(user, role);

        return new LoginResponse(user.Id, user.Name, user.Email, token, role);
    }
}
