
using Test.DbContext;
using Test.FactoryPattern;
using Test.Services.PaymentServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
namespace Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer("connection_string"));
            
           
            builder.Services.AddScoped<IStripeService, StripeServices>();
            builder.Services.AddScoped<IPayPalService, PayPalService>();
            builder.Services.AddScoped<PaymentGatewayFactory>();
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.Run();
        }
    }
}
