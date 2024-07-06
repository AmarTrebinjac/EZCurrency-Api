using CurrencyConverter.Models;
using CurrencyConverter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<List<CurrencyDetailsModel>>(builder.Configuration.GetSection("Currencies"));

builder.Services.AddSingleton<CurrencyService>();
builder.Services.AddHttpClient<CurrencyService>();

builder.Services.AddLogging(opt =>
opt.AddConfiguration(builder.Configuration.GetSection("Logging"))
  .AddConsole()
  .AddSentry()
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
