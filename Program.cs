using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1) سلسلة الاتصال (تأكد من وجودها في appsettings.json)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2) DbContext مع MySQL (Pomelo)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 👇 استخدم ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>() // نضيف الدعم للأدوار
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 4) خط أنابيب الطلبات (Middleware)
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

// لملفات wwwroot
app.UseStaticFiles();

app.UseRouting();

// مهم قبل التفويض
app.UseAuthentication();
app.UseAuthorization();
// يضيف دور Citizen تلقائيًا لكل مستخدم مسجّل ليس Admin
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);
        if (user != null)
        {
            var isAdmin = await userManager.IsInRoleAsync(user, Roles.Admin);
            var isCitizen = await userManager.IsInRoleAsync(user, Roles.Citizen);

            if (!isAdmin && !isCitizen)
            {
                await userManager.AddToRoleAsync(user, Roles.Citizen);
            }
        }
    }
    await next();
});


// لدعم أصول الستاتيك القادمة من مشاريع/حزم أخرى (.WithStaticAssets)
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

    // 1) إنشاء الأدوار إن لم تكن موجودة
    foreach (var roleName in new[] { Roles.Admin, Roles.Citizen })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    // 2) إنشاء حساب أدمن أولي (عدّل الإيميل والباسورد)
    string adminEmail = "admin@gov.local";
    string adminPass = "Admin#12345"; // غيّره لاحقًا!
    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(admin, adminPass);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
        else
        {
            // سجل الأخطاء في اللوج لو أردت
        }
    }
}
// ====== نهاية التهيئة ======

app.Run();
