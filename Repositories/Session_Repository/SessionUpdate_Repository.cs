using Commons;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Services.SessionServices;

namespace Session_Repository;

public class SessionUpdate_Repository : ISessionCommandUpdate
{
  private readonly ApplicationDbContext _context;  private readonly ILogger<SessionUpdate_Repository> _log;

  public SessionUpdate_Repository(ApplicationDbContext context,ILogger<SessionUpdate_Repository> log)
  {
    _context = context;
    _log = log;
  }

  public async Task<ApiResponse<int>> RevokeAllSessions(int userId)
  {
    try
    {
      var sessions = await _context.Sessions.Where(s => s.UserId == userId && !s.IsRevoke).ToListAsync();

      if (sessions == null || sessions.Count == 0)
      {
        return new ApiResponse<int>(false, "No sessions found for this user");
      }

      foreach (var session in sessions)
      {
        session.IsRevoke = true;
        session.ExpireTokenRequest = DateTime.UtcNow;
        session.UpdateAt = DateTime.UtcNow;
      }

      await _context.SaveChangesAsync();
      return new ApiResponse<int>(true, "All sessions revoked successfully", sessions.Count);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error revoking all sessions for user {UserId}", userId);
      return new ApiResponse<int>(false, "An error occurred while revoking sessions", 0);
    }
  }

  public async Task<ApiResponse<bool>> RevokeSession(int sessionId)
  {
    try
    {
      var session = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId);

      if (session == null)
      {
        return new ApiResponse<bool>(false, "Session not found", false);
      }

      session.IsRevoke = true;
      session.ExpireTokenRequest = DateTime.UtcNow;
      session.UpdateAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();

      return new ApiResponse<bool>(true, "Session revoked successfully", true);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error revoking session");
      return new ApiResponse<bool>(false, "An error occurred while revoking session", false);
    }
  }
}