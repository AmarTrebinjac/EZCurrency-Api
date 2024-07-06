using CurrencyConverter.Models;
using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Controllers;

[ApiController]
[Route("[controller]")]
public class CurrenciesController(CurrencyService currencyService, ILogger<CurrenciesController> logger) : ControllerBase
{
    private readonly CurrencyService _currencyService = currencyService;
    private readonly ILogger<CurrenciesController> _logger = logger;

    [HttpGet]
    public ActionResult<CurrencyDetailsModel[]> Get()
    {
        _logger.LogInformation("Returned all supported currencies");
        return Ok(_currencyService.GetSupportedCurrencies);
    }

    [HttpGet("[action]")]
    public ActionResult<string[]> Codes()
    {
        _logger.LogInformation("Returned all supported currency codes");
        return Ok(_currencyService.GetSupportedCurrencyCodes);
    }
}