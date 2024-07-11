using CurrencyConverter.Models;
using CurrencyConverter.Services;

var builder = WebApplication.CreateBuilder(args);
var allowAll = "AllowAll";
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<List<CurrencyDetailsModel>>(builder.Configuration.GetSection("Currencies"));

builder.Services.AddSingleton<CurrencyService>();
builder.Services.AddHttpClient<CurrencyService>();

builder.Services.AddCors(options =>
{
  options.AddPolicy(allowAll,
          allowAll =>
          {
            allowAll
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
          });
});

builder.Services.AddLogging(opt =>
opt.AddConfiguration(builder.Configuration.GetSection("Logging"))
  .AddConsole()
  .AddSentry()
);

var app = builder.Build();

// Enable public swagger because the API is also public.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(allowAll);
app.UseAuthorization();

app.MapControllers();

app.Run();
