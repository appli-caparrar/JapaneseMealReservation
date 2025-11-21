using JapaneseMealReservation.Models;

namespace JapaneseMealReservation.ViewModels
{
    public class MenuViewModel
    {
        public List<Menu> Menus { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
