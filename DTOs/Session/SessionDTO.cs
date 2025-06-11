using System.ComponentModel.DataAnnotations;

namespace DTOs.Session;

public class SessionDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenRequest { get; set; } = null!;
    public DateTime ExpireTokenRequest { get; set; }
    public string TokenRefresh { get; set; } = null!;
    public DateTime ExpireTokenRefresh { get; set; }
    public string IpAddress { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string Device { get; set; } = null!;
    public bool IsRevoke { get; set; } = true;
}