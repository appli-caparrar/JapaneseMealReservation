using JapaneseMealReservation.Models;
using JapaneseMealReservation.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace JapaneseMealReservation.AppData
{
    //Postgres Db Context
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<AdvanceOrder> AdvanceOrders { get; set; }
        public DbSet<OrderSummaryViewModel> OrderSummaryView { get; set; }
        public DbSet<CombineOrder> CombineOrders { get; set; }

        public DbSet<AccessToken> AccessTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderSummaryViewModel>().HasNoKey().ToView("order_summary");
            modelBuilder.Entity<CombineOrder>().HasNoKey().ToView("combine_order");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    // Apply DateTime UTC conversion only to DateTime types
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                        ));
                    }
                }
            }


            base.OnModelCreating(modelBuilder);
        }
    }
}
