using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    public class Employee
    {
        [Key]
        [Column("EmpNo")]
        public string? EmpNo { get; set; }
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }
        public string? Section { get; set; }
        public string? Email { get; set; }
        public string Position { get; set; }
        public string ADID { get; set; }
    }
}
