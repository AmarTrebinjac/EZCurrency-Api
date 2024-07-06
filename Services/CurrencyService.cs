
using System.Globalization;
using System.Text.Json;
using CurrencyConverter.Models;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Services;
public class CurrencyService(IOptions<List<CurrencyDetailsModel>> supportedCurrencies, HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly List<CurrencyDetailsModel> _supportedCurrencies = supportedCurrencies.Value;

    public string[] GetSupportedCurrencyCodes => _supportedCurrencies.Select(c => c.Code).ToArray();
    public List<CurrencyDetailsModel> GetSupportedCurrencies => _supportedCurrencies;

    public async Task<List<ConvertedCurrencyModel>> GetCurrencyRatesInNOK(string[]? selectedCurrencies = null)
    {
        selectedCurrencies ??= GetSupportedCurrencyCodes;

        var json = await GetExternalCurrencies(selectedCurrencies);
        var convertedCurrencies = ConvertToNOK(json);
        return convertedCurrencies;
    }

    /// <summary>
    /// The API is case sensitive, so always make sure the currency codes are in uppercase.
    /// </summary>
    private async Task<JsonElement> GetExternalCurrencies(string[] currencies)
    {
        var res = await _httpClient.GetAsync($"https://data.norges-bank.no/api/data/EXR/B.{String.Join("+", currencies)}.NOK.SP?format=sdmx-json&lastNObservations=1&locale=en");

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get currency rates");
        }

        var content = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    private static List<ConvertedCurrencyModel> ConvertToNOK(JsonElement json)
    {
        var convertedCurrencies = new List<ConvertedCurrencyModel>();
        var unitMeasurements = json.GetProperty("data")
        .GetProperty("structure")
        .GetProperty("attributes")
        .GetProperty("series")
        .EnumerateArray()
        .First(attr => attr.GetProperty("id").GetString() == "UNIT_MULT")
        .GetProperty("values")
        .EnumerateArray();

        var series = json.GetProperty("data")
        .GetProperty("dataSets")
        .EnumerateArray()
        .First()
        .GetProperty("series")
        .EnumerateObject()
        .ToArray();

        var baseCurrencies = json.GetProperty("data")
        .GetProperty("structure")
        .GetProperty("dimensions")
        .GetProperty("series")
        .EnumerateArray()
        .First(attr => attr.GetProperty("id").GetString() == "BASE_CUR")
        .GetProperty("values")
        .EnumerateArray();

        for (var i = 0; i < series.Count(); i++)
        {
            // A series is the data structure for a currency, containing the observations and attribute to determine the unit of measurement.
            var currentSeries = series.ElementAt(i);
            var observation = Decimal.Parse(currentSeries.Value.GetProperty("observations").EnumerateObject().First().Value.EnumerateArray().First().ToString(), CultureInfo.InvariantCulture);

            // The third attribute in the series is the unit of measurement.
            var measurementAttr = currentSeries.Value.GetProperty("attributes").EnumerateArray().ElementAt(2).GetInt32();
            // The measurement id tells us what the unit of measurement is.
            var measurementId = unitMeasurements.ElementAt(measurementAttr).GetProperty("id").GetString();
            var unitValue = GetUnitValue(observation, measurementId!);

            convertedCurrencies.Add(new ConvertedCurrencyModel
            {
                Base = baseCurrencies.ElementAt(i).GetProperty("id").GetString()!,
                Quote = "NOK",
                Rate = unitValue
            });
        }

        // NOK gets ignored by the API as it is always the quote currency.
        // We get around this limitation by adding it manually, as we know the rate will always be 1.
        convertedCurrencies.Add(new ConvertedCurrencyModel
        {
            Base = "NOK",
            Quote = "NOK",
            Rate = 1
        });

        return convertedCurrencies;
    }

    /// <summary>
    /// Returns the unit value in cases where the observation is not measured in single units.
    /// </summary>
    /// <param name="quote">
    /// The observed quote value is always in NOK.
    /// </param>
    /// <param name="measurementId">
    /// ID 0 = 1 to 1 conversion. <para />
    /// ID 1 = 1 to 10 conversion. - Assumption as it's never been observed. <para />
    /// ID 2 = 1 to 100 conversion. <para />
    /// </param>
    /// <returns></returns>
    private static decimal GetUnitValue(decimal quote, string measurementId)
    {
        if (measurementId == "0")
            return quote;
        // Never actually observed this value, so it's an assumption.
        else if (measurementId == "1")
            return quote / 10;

        // Never seen the id go over 2, so the assumption is that it's the only other value.
        return quote / 100;
    }

    public ConvertedCurrencyModel GetRate(ConvertedCurrencyModel baseCurrency, ConvertedCurrencyModel quoteCurrency, decimal amount)
    {
        var rate = baseCurrency.Rate / quoteCurrency.Rate;
        return new ConvertedCurrencyModel
        {
            Base = baseCurrency.Base,
            Quote = quoteCurrency.Quote,
            Rate = amount * rate * 100 / 100
        };
    }
}