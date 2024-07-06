using CurrencyConverter.Models;

namespace CurrencyConverter.ViewModels
{
    public class ConversionViewModel
    {
        public string Code { get; set; } = null!;
        public decimal Amount { get; set; }
        public List<CurrencyRateModel> Conversions { get; set; } = [];
    }
}