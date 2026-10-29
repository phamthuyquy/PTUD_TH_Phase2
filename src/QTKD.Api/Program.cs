using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("QtkdDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'QtkdDatabase' is missing. Configure it with .NET user-secrets or an environment variable.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<QtkdDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICustomerReadService, CustomerReadService>();
builder.Services.AddScoped<IContractReadService, ContractReadService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "QTKD API v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program
{
}
