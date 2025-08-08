namespace JapaneseMealReservation.ViewModels
{
    public class ReservationViewModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string ReservationDate { get; set; }
        public string Time { get; set; } = string.Empty;
        public int Menu_Id { get; set; } 
        public string MenuType { get; set; } = string.Empty;
        public int Quantity { get; set; } 
    }
}
