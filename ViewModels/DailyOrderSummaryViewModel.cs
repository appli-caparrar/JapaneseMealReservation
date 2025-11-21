namespace JapaneseMealReservation.ViewModels
{
    public class DailyOrderSummaryViewModel
    {
        public List<OrderSummaryViewModel> BentoOrders { get; set; }
        public List<OrderSummaryViewModel> BreakfastOrders { get; set; }
        public List<OrderSummaryViewModel> CurryOrders { get; set; }
        public List<OrderSummaryViewModel> MakiOrders { get; set; }
        public List<OrderSummaryViewModel> NoodlesOrders { get; set; }
    }
}
