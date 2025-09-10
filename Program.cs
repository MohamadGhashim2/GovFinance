using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GovFinance.Services;


var builder = WebApplication.CreateBuilder(args);


var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrencyProvider, CookieCurrencyProvider>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>() 
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();
//var culture = new CultureInfo("ar-SY"); // أو ar-SA حسب عملتك
//var opts = new RequestLocalizationOptions
//{
//    DefaultRequestCulture = new RequestCulture(culture),
//    SupportedCultures = new[] { culture },
//    SupportedUICultures = new[] { culture }
//};
//app.UseRequestLocalization(opts);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
            var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();

            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (roles == null || roles.Count == 0)
                {
                    await userManager.AddToRoleAsync(user, Roles.User);
                    await signInManager.RefreshSignInAsync(user);
                }

                var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
                if (!isAdmin)
                {
                    var hasUser = await db.Userrs.AnyAsync(c => c.ApplicationUserId == user.Id);
                    if (!hasUser)
                    {
                        var tmpNid = ("N" + user.Id.Replace("-", "")).PadRight(11, '0').Substring(0, 11);
                        var fallbackName = user.Email?.Split('@').FirstOrDefault() ?? "User";
                        db.Userrs.Add(new User { ApplicationUserId = user.Id, UserId = tmpNid, FullName = fallbackName });
                        await db.SaveChangesAsync();
                    }
                }
            }
        }
        await next();
    });
}

app.UseAuthorization();


app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.MapRazorPages().WithStaticAssets();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var roleName in new[] { Roles.Admin, Roles.User })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }
}


app.Run();
