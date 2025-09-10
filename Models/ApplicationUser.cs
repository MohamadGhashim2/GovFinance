using Microsoft.AspNetCore.Identity;

namespace GovFinance.Models
{
    public class ApplicationUser : IdentityUser
    {
        public User? User { get; set; } 
    }
}
