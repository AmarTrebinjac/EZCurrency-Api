namespace CurrencyConverter.Models
{
    public class ConvertedCurrencyModel
    {
        public string Base { get; set; } = null!;
        public string Quote { get; set; } = null!;
        public decimal Rate { get; set; }
    }
}