using CloudShield.Commons;

namespace Entities.Users;
public class Sessions: BaseAbstract
{
    public Guid UserId { get; set; }
    public string TokenRequest { get; set; }
    public DateTime ExpireTokenRequest { get; set; }
    public string TokenRefresh { get; set; }
    public DateTime ExpireTokenRefresh { get; set; }
    public string IpAddress { get; set; }
    public string Location { get; set; }
    public string Device { get; set; }
    public bool IsRevoke { get; set; } = true;
    public required virtual User User { get; set; }
}