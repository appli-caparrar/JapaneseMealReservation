using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    public class AdvanceOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? EmployeeId { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }

        public string? Section { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        public string? MealTime { get; set; }

        [Required]
        public string? MenuType { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? CustomerType { get; set; }
        public string Status { get; set; } = "Pending"; // Default value

        //[Required]
        //public int Menu_Id { get; set; }
    }
}
