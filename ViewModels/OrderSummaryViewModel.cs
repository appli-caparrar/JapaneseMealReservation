namespace JapaneseMealReservation.ViewModels
{
    public class OrderSummaryViewModel
    {
        public string ReferenceNumber { get; set; }
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Section { get; set; }
        public string email { get; set; }
        public string CustomerType { get; set; }
        public DateTime ReservationDate { get; set; }
        public string MealTime { get; set; }
        public string MenuType { get; set; }
        public string Status { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public decimal Total => Quantity * Price;
    }
}
