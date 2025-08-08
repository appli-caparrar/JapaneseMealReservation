using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    public class AccessToken
    {
        [Key]
        public Guid Token { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }
    }
}
