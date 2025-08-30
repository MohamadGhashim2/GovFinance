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

        public DbSet<Citizen> Citizens => Set<Citizen>();
        public DbSet<Income> Incomes => Set<Income>();
        public DbSet<Expense> Expenses => Set<Expense>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Citizen
            modelBuilder.Entity<Citizen>(b =>
            {
                b.HasIndex(x => x.NationalId).IsUnique(); // رقم وطني فريد
                b.Property(x => x.FullName).HasMaxLength(150).IsRequired();

                // واحد-لواحد مع ApplicationUser
                b.HasOne(x => x.ApplicationUser)
                 .WithOne(u => u.Citizen)
                 .HasForeignKey<Citizen>(x => x.ApplicationUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Income
            modelBuilder.Entity<Income>(b =>
            {
                b.Property(x => x.Amount).HasPrecision(18, 2);
                b.Property(x => x.Date).HasColumnType("date"); // MySQL DATE
                b.HasIndex(x => new { x.CitizenId, x.Date });
            });

            // Expense
            modelBuilder.Entity<Expense>(b =>
            {
                b.Property(x => x.Amount).HasPrecision(18, 2);
                b.Property(x => x.Date).HasColumnType("date");
                b.HasIndex(x => new { x.CitizenId, x.Date });
            });
        }
    }
}
