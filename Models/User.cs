using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("employee_id")]
        [MaxLength(50)]
        [Display(Name = "Employee Id")]
        public string? EmployeeId { get; set; }

        [Required]
        [Column("first_name")]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Required]
        [Column("last_name")]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [Column("section")]
        [MaxLength(50)]
        public string? Section { get; set; } //New added

        [Required]
        [Column("password")]
        [DataType(DataType.Password)]
        [MaxLength(255)] // assuming you will hash the password
        public string? Password { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;


        [Column("user_role")]
        [MaxLength(50)]
        [Display(Name = "User Role")]
        public string? UserRole { get; set; } //Newly added
        public string? EmployeeType { get; set; } //Newly added Expat or Local
        
        public string? Position { get; set; }

        [NotMapped]
        public string? ADID { get; set; }

    }
}
