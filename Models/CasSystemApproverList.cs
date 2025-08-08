using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    [Table("Tbl_System_Approver_list")]
    public class CasSystemApproverList
    {
        [Key]  // <- This line is required
        [Column("ID")]

        public int Id { get; set; }  // Or whatever your primary key is
        [Column("SYSTEM ID")]

        public string? SystemID { get; set; }

        [Column("SYSTEM NAME")]
        public string? SystemName { get; set; }

        [Column("APPROVER NUMBER")]
        public string? ApproverNumber { get; set; }

        [Column("FULL NAME")]
        public string? FullName { get; set; }

        [Column("EMAIL ADDRESS")]
        public string? EmailAddress { get; set; }

        public string? SECTION { get; set; }

        public string? POSITION { get; set; }

        public string? ADID { get; set; }

        [Column("EMPLOYEE NUMBER")]
        public string? EmployeeNumber { get; set; }
    }
}
