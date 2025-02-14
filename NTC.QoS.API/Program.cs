using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Writers;
using NTC.QoS.Application;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddApplication(builder.Configuration);

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                s.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
            });

            
            
        }

        
        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
