namespace JapaneseMealReservation.ViewModels
{
    public class ExpatMonthlyDeduction
    {
        public string Name { get; set; } = string.Empty;

        public decimal ExpatBento { get; set; }
        public decimal ExpatCurryRice { get; set; }
        public decimal ExpatNoodles { get; set; }
        public decimal MakiRoll { get; set; }
        public decimal Breakfast { get; set; }


        public decimal TotalAmount =>
            ExpatBento + ExpatCurryRice + ExpatNoodles + MakiRoll + Breakfast;
    }
}
