namespace JapaneseMealReservation.ViewModels
{
    public class ReservedOrderViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SelectedStatus { get; set; }

        // Must be strongly typed
        public List<ReservationViewModel> OrderSummaries { get; set; } = new List<ReservationViewModel>();
    }


}
