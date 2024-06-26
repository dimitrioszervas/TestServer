using TestServer.Server;

/*
No need to do migrations you use the TestServer's migrations
 
Only create/update the databases 

update-database -Context EndocloudDbContext
update-database -Context PublicDbContext

 */

namespace Server1
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

            Servers.Inst.LoadSettings1();
            CryptoUtils.GenerateOwnerKeys();

            app.Run();
        }
    }
}