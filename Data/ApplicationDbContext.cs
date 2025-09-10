using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GovFinance.Models;

namespace GovFinance.Data
{
    // مهم: نجعل DbContext مبني على ApplicationUser (وليس IdentityUser)
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Userrs => Set<User>();
        public DbSet<Income> Incomes => Set<Income>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<IncomeCategory> IncomeCategories => Set<IncomeCategory>();
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(b =>
            {
                b.HasIndex(x => x.UserId).IsUnique(); // رقم وطني فريد
                b.Property(x => x.FullName).HasMaxLength(150).IsRequired();

                // واحد-لواحد مع ApplicationUser
                b.HasOne(x => x.ApplicationUser)
                 .WithOne(u => u.User)
                 .HasForeignKey<User>(x => x.ApplicationUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Income
            // Data/ApplicationDbContext.cs  داخل OnModelCreating
            modelBuilder.Entity<Income>(b =>
            {
                b.Property(x => x.Amount).HasPrecision(18, 2);
                b.Property(x => x.CollectedAmount).HasPrecision(18, 2);   // جديد
                b.Property(x => x.Date).HasColumnType("date");

                b.HasOne(x => x.IncomeCategory)
                 .WithMany()
                 .HasForeignKey(x => x.IncomeCategoryId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Data/ApplicationDbContext.cs  داخل OnModelCreating
            modelBuilder.Entity<Expense>(b =>
            {
                b.Property(x => x.Amount).HasPrecision(18, 2);
                b.Property(x => x.PaidAmount).HasPrecision(18, 2);   // جديد
                b.Property(x => x.Date).HasColumnType("date");

                b.HasOne(x => x.ExpenseCategory)
                 .WithMany()
                 .HasForeignKey(x => x.ExpenseCategoryId)
                 .OnDelete(DeleteBehavior.SetNull);
            });



            modelBuilder.Entity<IncomeCategory>(b =>
            {
                b.Property(x => x.DefaultAmount).HasPrecision(18, 2);
                b.HasIndex(x => new { x.UserId, x.Name }).IsUnique(); // لا تتكرر نفس التسمية لنفس المستخدم
                b.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(x => x.LinkedExpenseCategory)
     .WithMany()
     .HasForeignKey(x => x.LinkedExpenseCategoryId)
     .OnDelete(DeleteBehavior.SetNull); // لو حذفنا المصروف تبقى الفئة بلا ربط
            });
            modelBuilder.Entity<ExpenseCategory>(b =>
            {
                b.Property(x => x.DefaultAmount).HasPrecision(18, 2);
                b.HasIndex(x => new { x.UserId, x.Name }).IsUnique(); // لا تتكرر نفس التسمية لنفس المستخدم
                b.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
