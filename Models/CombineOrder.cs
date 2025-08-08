using Microsoft.EntityFrameworkCore;

namespace JapaneseMealReservation.Models
{
    [Keyless]
    public class CombineOrder
    {
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Section { get; set; }
        public int Quantity { get; set; }
        public DateTime ReservationDate { get; set; }
        public string MealTime { get; set; }
        public string MenuType { get; set; }
        public string ReferenceNumber { get; set; }
        public string CustomerType { get; set; }
        public string Source { get; set; } // "Order" or "AdvanceOrder"
    }
}
