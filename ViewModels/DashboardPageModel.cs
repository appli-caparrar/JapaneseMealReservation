using JapaneseMealReservation.Models;

namespace JapaneseMealReservation.ViewModels
{
    public class DashboardPageModel
    {
        //Models
        public List<Menu>? Menus { get; set; }
        public Order? Order { get; set; }

        //Views
        public int TotalBentoToday { get; set; }
        public int TotalMakiToday { get; set; }
        public int TotalCurryToday { get; set; }
        public int TotalNoodlesToday { get; set; }
        public int TotalBreakfastToday { get; set; }
    }
}
