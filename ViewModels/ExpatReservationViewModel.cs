using JapaneseMealReservation.Models;

namespace JapaneseMealReservation.ViewModels
{
    public class ExpatReservationViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<DateTime> CurrentMonthDates { get; set; } = new();
        public Dictionary<int, List<string>> WeekdayMenus { get; set; } = new();
        public string MealTime { get; set; }
        public string ReferenceNumber { get; set; }
        public string CurrentUserId { get; set; }

        // Format: userId|yyyy-MM-dd|Menu
        public List<string> SelectedOrders { get; set; } = new();
        public List<string> ReservedDates { get; set; } = new();
        // Format: userId|yyyy-MM-dd => Menu (e.g., "Bento", "Ramen")
        public Dictionary<string, string> ReservedOrders { get; set; } = new();
        
    }
}
