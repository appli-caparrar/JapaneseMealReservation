using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace JapaneseMealReservation.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string? EmployeeId { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }

        public string? Section { get; set; }

        ////[Required]
        //[MaxLength(50)]
        //public string? ReferenceNumber { get; set; }

        // New Fields
        public string? OrderName { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        public TimeSpan? MealTime { get; set; }

        public string? ReferenceNumber { get; set; }
        public string? MenuType { get; set; }
        public int Menu_Id { get; set; }
        public string? CustomerType { get; set; }

        public string Status { get; set; } = "Pending"; // Default value

        //public int Price { get; set; }
        //public int TotalAmount { get; set; }
    }
}
