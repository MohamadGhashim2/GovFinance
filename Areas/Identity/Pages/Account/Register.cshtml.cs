using System.ComponentModel.DataAnnotations;
using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GovFinance.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IConfiguration config,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _config = config;
            _db = db;
        }

        [BindProperty] public InputModel Input { get; set; } = new();

        // يأتي من query string ويُحفظ hidden بالصفحة
        [BindProperty] public string? Role { get; set; }

        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = default!;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = default!;

            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = default!;

            // يُستخدم فقط عند التسجيل كـ Admin
            public string? AdminInviteCode { get; set; }
        }

        public async Task OnGetAsync(string? role = null, string? returnUrl = null)
        {
            Role = role ?? Roles.Citizen;
            ReturnUrl = returnUrl ?? "/";
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl ??= returnUrl ?? "/";
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            var selectedRole = (Role?.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase) == true)
                ? Roles.Admin
                : Roles.Citizen;

            if (!ModelState.IsValid)
                return Page();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true // للتطوير
            };

            var createRes = await _userManager.CreateAsync(user, Input.Password);
            if (!createRes.Succeeded)
            {
                foreach (var e in createRes.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            // تأكد من وجود الدور
            if (!await _roleManager.RoleExistsAsync(selectedRole))
                await _roleManager.CreateAsync(new IdentityRole(selectedRole));

            // تحقق من كود الإدارة لو Admin
            if (selectedRole == Roles.Admin)
            {
                var requiredCode = _config["Security:AdminAccessCode"]?.Trim();
                var providedCode = Input.AdminInviteCode?.Trim();

                if (string.IsNullOrEmpty(requiredCode) ||
                    !string.Equals(providedCode, requiredCode, StringComparison.Ordinal))
                {
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError(string.Empty, "رمز الإدارة غير صحيح.");
                    return Page();
                }
            }

            var addRes = await _userManager.AddToRoleAsync(user, selectedRole);
            if (!addRes.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var msg = string.Join("; ", addRes.Errors.Select(e => e.Description));
                ModelState.AddModelError(string.Empty, "تعذّر إسناد الدور: " + msg);
                return Page();
            }

            // أنشئ سجل Citizen تلقائيًا عند التسجيل كمواطن (NationalId مطلوب => نولّد قيمة فريدة مؤقتة)
            if (selectedRole == Roles.Citizen)
            {
                var exists = await _db.Citizens.AnyAsync(c => c.ApplicationUserId == user.Id);
                if (!exists)
                {
                    // NationalId مؤقت فريد بطول 11 (مشتق من UserId)
                    var tmpNid = ("N" + user.Id.Replace("-", "")).PadRight(11, '0').Substring(0, 11);

                    var fallbackName = user.Email?.Split('@').FirstOrDefault() ?? "Citizen";
                    _db.Citizens.Add(new Citizen
                    {
                        ApplicationUserId = user.Id,
                        NationalId = tmpNid,
                        FullName = fallbackName
                    });
                    await _db.SaveChangesAsync();
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("User registered with role {Role}", selectedRole);

            var redirect = string.IsNullOrWhiteSpace(ReturnUrl) || ReturnUrl == "/"
                ? (selectedRole == Roles.Admin ? "/Admin" : "/MyExpenses")
                : ReturnUrl;

            return LocalRedirect(redirect);
        }
    }
}
