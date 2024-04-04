using TestServer.Configurations;
using TestServer.Contracts;
using TestServer.Contracts.Private;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Repositories;
using TestServer.Repositories.Private;
using TestServer.Repositories.Public;
using TestServer.Server;
using TestServer.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

/*
No need to do migrations you use the TestServer's migrations
 
Only create/update the databases 

update-database -Context EndocloudDbContext
update-database -Context PublicDbContext
 */

namespace Server2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(x => x
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                                .SetIsOriginAllowed(origin => true) // allow any origin
                                                                    //.WithOrigins("https://localhost:44351")); // Allow only this origin can also have multiple origins separated with comma
                                .AllowCredentials());
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();


            CryptoUtils.GenerateOwnerKeys();

            app.Run();
        }
    }
}
