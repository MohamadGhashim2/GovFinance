using Microsoft.AspNetCore.Identity;

namespace GovFinance.Models
{
    // حساب النظام (مواطن أو أدمن)
    public class ApplicationUser : IdentityUser
    {
        public Citizen? Citizen { get; set; } // علاقة واحد-لواحد مع Citizen
    }
}
