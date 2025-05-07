using Microsoft.AspNetCore.Identity;

namespace MedVoll.WebAPI.Models
{
    public class VollMedUser : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime? ExpireTime { get; set; }
    }
}
