using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JapaneseMealReservation.Models
{
    public class Tbl_LOGIN_Request
    {
        [Key] //primary key for EF Core tracking
        [Column("ID")]
        public int Id { get; set; }

        [Column("HOSTNAME")]
        public string? IpAddress { get; set; }

        [Column("SYSTEM ID")]
        public long SystemId { get; set; }
        
        [Column("USERNAME")]
        public string? EmployeeId { get; set; }

        [Column("STATUS")]
        public string? Status { get; set; }
    }
}
