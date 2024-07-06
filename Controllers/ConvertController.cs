using CurrencyConverter.Models;
using CurrencyConverter.Services;
using CurrencyConverter.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConvertController(CurrencyService currencyService, ILogger<ConvertController> logger) : ControllerBase
    {
        private readonly CurrencyService _currencyService = currencyService;
        private readonly ILogger<ConvertController> _logger = logger;

        [HttpGet("Convert-All-To/{currency}/Amount/{amount}")]
        public async Task<ActionResult> ConvertAllTo(string currency, decimal amount)
        {
            try
            {
                currency = currency.ToUpper();

                if (!_currencyService.GetSupportedCurrencyCodes.Contains(currency))
                    BadRequest("Invalid Currency");

                if (amount <= 0)
                    BadRequest("Invalid Amount");

                var convertedToNOK = await _currencyService.GetCurrencyRatesInNOK(_currencyService.GetSupportedCurrencyCodes);

                if (convertedToNOK is null)
                {
                    return StatusCode(500, "Failed to convert currencies");
                }

                var vm = new List<ConversionViewModel>();

                var from = convertedToNOK.First(c => c.Base == currency);
                var conversion = new ConversionViewModel { Code = currency, Amount = amount };

                var targetCurrencies = _currencyService.GetSupportedCurrencyCodes.Where(c => c != currency);
                foreach (var toCurrency in targetCurrencies)
                {
                    var to = convertedToNOK.First(c => c.Base == toCurrency);
                    conversion.Conversions.Add(new CurrencyRateModel { Code = to.Base, Rate = _currencyService.GetRate(from, to, amount).Rate });
                }

                vm.Add(conversion);

                _logger.LogInformation($"Successfully converted from {currency} to all other currencies");
                return Ok(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert currencies");
                return StatusCode(500, "Failed to convert currencies");
            }
        }

        [HttpGet("From/{fromCurrencies}/To/{toCurrencies}/Amount/{amount}")]
        public async Task<ActionResult> ConvertMultiple(string fromCurrencies, string toCurrencies, decimal amount)
        {
            try
            {
                var supportedCurrencies = _currencyService.GetSupportedCurrencyCodes;
                var fromCurrenciesArray = fromCurrencies.Split(",").Select(c => c.ToUpper()).Where(c => supportedCurrencies.Contains(c)).ToArray();
                var toCurrenciesArray = toCurrencies.Split(",").Select(c => c.ToUpper()).Where(c => supportedCurrencies.Contains(c)).ToArray();

                if (!fromCurrenciesArray.Any(curr => supportedCurrencies.Contains(curr.ToUpper())) || !toCurrenciesArray.Any(curr => supportedCurrencies.Contains(curr.ToUpper())))
                    return BadRequest("Invalid input");


                var convertedToNOK = await _currencyService.GetCurrencyRatesInNOK(fromCurrenciesArray.Concat(toCurrenciesArray).Distinct().ToArray());

                var vm = new List<ConversionViewModel>();

                foreach (var fromCurrency in fromCurrenciesArray)
                {
                    var from = convertedToNOK.First(c => c.Base == fromCurrency);
                    var conversion = new ConversionViewModel { Code = fromCurrency, Amount = amount };
                    var targetCurrencies = toCurrenciesArray.Where(c => c != fromCurrency);

                    foreach (var toCurrency in targetCurrencies)
                    {
                        var to = convertedToNOK.First(c => c.Base == toCurrency);
                        conversion.Conversions.Add(new CurrencyRateModel { Code = to.Base, Rate = _currencyService.GetRate(from, to, amount).Rate });
                    }
                    vm.Add(conversion);
                }

                _logger.LogInformation($"Successfully converted currencies from {fromCurrencies} to {toCurrencies}");
                return Ok(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert currencies");
                return StatusCode(500, "Failed to convert currencies");
            }
        }
    }
}