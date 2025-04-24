using AutoMapper;
using Commons;
using DataContext;
using DTOs.Session;
using Microsoft.EntityFrameworkCore;
using Services.SessionServices;

namespace Session_Repository;

public class SessionRead_Repository : ISessionCommandRead
{
  private readonly ApplicationDbContext _context;
  private readonly IMapper _mapper;
  private readonly ILogger<SessionRead_Repository> _log;
  public SessionRead_Repository(ApplicationDbContext context, IMapper mapper, ILogger<SessionRead_Repository> log)
  {
    _mapper = mapper;
    _log = log;
    _context = context;
  }
  public async Task<ApiResponse<List<SessionDTO>>> GetAll()
  {
    try
    {
      var sessions = await _context.Sessions.ToListAsync();

      if (sessions == null || sessions.Count == 0)
      {
        return new ApiResponse<List<SessionDTO>>(false, "No sessions found", null);
      }

      var sessionsDTOs = _mapper.Map<List<SessionDTO>>(sessions);
      return new ApiResponse<List<SessionDTO>>(true, "Sessions retrieved successfully", sessionsDTOs);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving sessions");
      return new ApiResponse<List<SessionDTO>>(false, "An error occurred while retrieving sessions", null);
    }
  }

  public async Task<ApiResponse<SessionDTO>> GetById(int sessionId)
  {
    try
    {
      var sessionById = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId);

      if (sessionById == null)
      {
        return new ApiResponse<SessionDTO>(false, "Session not found", null);
      }

      var SessionDTO = _mapper.Map<SessionDTO>(sessionById);
      return new ApiResponse<SessionDTO>(true, "Session retrieved successfully", SessionDTO);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving session by ID");
      return new ApiResponse<SessionDTO>(false, "An error occurred while retrieving session", null);
    }
  }

  public async Task<ApiResponse<List<SessionDTO>>> GetByUserId(int userId)
  {
    try
    {
      var sessionsByUserId = await _context.Sessions.Where(x => x.UserId == userId).ToListAsync();

      if (sessionsByUserId == null || sessionsByUserId.Count == 0)
      {
        return new ApiResponse<List<SessionDTO>>(false, "No sessions found for this user", null);
      }

      var sessionDTOs = _mapper.Map<List<SessionDTO>>(sessionsByUserId);
      return new ApiResponse<List<SessionDTO>>(true, "Sessions retrieved successfully", sessionDTOs);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving sessions by user ID");
      return new ApiResponse<List<SessionDTO>>(false, "An error occurred while retrieving sessions", null);
    }
  }

  public async Task<ApiResponse<SessionDTO>> GetByToken(string token)
  {
    try
    {
      var session = await _context.Sessions
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.TokenRequest == token);

        if (session == null || session.IsRevoke || session.ExpireTokenRequest < DateTime.UtcNow)
        {
          return new ApiResponse<SessionDTO>(false, "Invalid or expired session token", null);
        }

        return new ApiResponse<SessionDTO>(true, "Session found", _mapper.Map<SessionDTO>(session));
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving session by token");
      return new ApiResponse<SessionDTO>(false, "An error occurred while retrieving session", null);
    }
  }
}