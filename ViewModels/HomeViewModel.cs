using JapaneseMealReservation.Models;

namespace JapaneseMealReservation.ViewModels
{
    public class HomeViewModel
    {
        public User User { get; set; }
        public List<Menu> WeeklyMenus { get; set; } = new();
    }
}
