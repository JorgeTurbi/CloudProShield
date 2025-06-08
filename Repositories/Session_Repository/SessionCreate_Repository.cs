using AutoMapper;
using Commons;
using DataContext;
using DTOs.Session;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.SessionServices;
using Services.TokenServices;

namespace Session_Repository
{
  public class SessionCreate_Repository : ISessionCommandCreate
  {
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SessionCreate_Repository> _log;
    private readonly ITokenService _token;

    public SessionCreate_Repository(ApplicationDbContext context, IMapper mapper, ILogger<SessionCreate_Repository> log, ITokenService tokenService)
    {
      _context = context;
      _mapper = mapper;
      _log = log;
      _token = tokenService;
    }

    public async Task<ApiResponse<SessionDTO>> CreateSession(Guid userId, string ip, string device)
    {
      try
      {
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
          return new ApiResponse<SessionDTO>(false, "User not found");

        var token = _token.IssueSessionResetToken(userId, user.Email, user.Name, TimeSpan.FromDays(1));
        var tokenRefresh = _token.IssueSessionResetToken(userId, user.Email, user.Name, TimeSpan.FromDays(7));

        var session = new Sessions
        {
          UserId = userId,
          User = user,
          TokenRequest = token,
          ExpireTokenRequest = DateTime.UtcNow.AddDays(1),
          TokenRefresh = tokenRefresh,
          ExpireTokenRefresh = DateTime.UtcNow.AddDays(3),
          IpAddress = ip,
          Location = "Unknown",
          Device = device,
          IsRevoke = false,
          CreateAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var dto = _mapper.Map<SessionDTO>(session);
        return new ApiResponse<SessionDTO>(true, "Session created", dto);
      }
      catch (Exception ex)
      {
        _log.LogError(ex, "Error creating session");
        return new ApiResponse<SessionDTO>(false, "Could not create session");
      }
    }
  }
}
