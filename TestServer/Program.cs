using TestServer.Server;
using Microsoft.EntityFrameworkCore;
using Serilog;

/*
Add-Migration InitialCreate -Context EndocloudDbContext -OutputDir Migrations\Private
Add-Migration InitialCreate -Context PublicDbContext -OutputDir Migrations\Public

update-database -Context EndocloudDbContext
update-database -Context PublicDbContext

 */

namespace TestServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Add Db and Db _context
            

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
           

            // Create a cors policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                                       builder =>
                                       {
                                           builder.AllowAnyOrigin()
                                                  .AllowAnyMethod()
                                                  .AllowAnyHeader();
                                       });
            });

            // Serilog config
            builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().ReadFrom.Configuration(ctx.Configuration));

           
            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // tell the app to record the requests in serilog
            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();

            // activate the cors policy
            app.UseCors("AllowAllOrigins");

            app.UseAuthorization();

            app.MapControllers();         

            app.Run();
        }
    }
}