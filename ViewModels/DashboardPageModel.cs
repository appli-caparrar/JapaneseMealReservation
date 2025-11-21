using JapaneseMealReservation.Models;

namespace JapaneseMealReservation.ViewModels
{
    public class DashboardPageModel
    {
        //For Filtering of Menu
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        //Models
        public List<Menu>? Menus { get; set; }
        public Order? Order { get; set; }

        //Views
        public int TotalBentoToday { get; set; }
        public int TotalMakiToday { get; set; }
        public int TotalCurryToday { get; set; }
        public int TotalNoodlesToday { get; set; }
        public int TotalBreakfastToday { get; set; }
        public int TotalOrderToday { get; set; }
       
    }
}
