using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }

        public DateTime? AvailabilityDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; } 

        public string? ImagePath { get; set; }

        public string? MenuType { get; set; } //Newly Add

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
