namespace JapaneseMealReservation.ViewModels
{
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public DateTime? AvailabilityDate { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public string MenuType { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public decimal Price { get; set; }
    }
}
