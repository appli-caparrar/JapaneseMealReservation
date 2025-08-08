using DocumentFormat.OpenXml.Spreadsheet;
using JapaneseMealReservation.Models;
using Microsoft.EntityFrameworkCore;

namespace JapaneseMealReservation.AppData
{
    public class SqlServerDbContext : DbContext
    {
        public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : base(options)
        {
          
        }

        public DbSet<Employee> View_UserInfo { get; set; }
        public DbSet<CasSystemApproverList> Tbl_System_Approver_list { get; set; }
        public DbSet<Tbl_LOGIN_Request> Tbl_LOGIN_Request { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasNoKey().ToView("View_UserInfo");
            modelBuilder.Entity<CasSystemApproverList>().ToTable("Tbl_System_Approver_list");
        }
    }

}
