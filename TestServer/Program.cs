using TestServer.Configurations;
using TestServer.Contracts;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Repositories.Public;
using TestServer.Repositories.Private;
using TestServer.Server;
using TestServer.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TestServer.Repositories;
using TestServer.Contracts.Public;
using System.Diagnostics.Contracts;
using TestServer.Configurations;
using TestServer.Contracts.Private;
using TestServer.Contracts.Public;
using TestServer.Contracts;
using TestServer.Models;
using TestServer.Repositories.Private;
using TestServer.Repositories.Public;
using TestServer.Repositories;
using TestServer.Server;
using TestServer.Services;

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
            var connectionString = builder.Configuration.GetConnectionString("EndocloudDbConnectionString");
            builder.Services.AddDbContext<EndocloudDbContext>(options => options.UseSqlServer(connectionString));

            var publicConnectionString = builder.Configuration.GetConnectionString("PublicDbConnectionString");
            builder.Services.AddDbContext<PublicDbContext>(options => options.UseSqlServer(publicConnectionString));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IServerService, ServerService>();

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

            // Add AutoMapper and config
            builder.Services.AddAutoMapper(typeof(MapperConfig));

            // Add repository and unit of work
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IServerService, ServerService>();

            // Add Private repository and unit of work
            builder.Services.AddScoped<IAuditsRepository, AuditsRepository>();
            builder.Services.AddScoped<IAttributesRepository, AttributesRepository>();
            builder.Services.AddScoped<IApprovalsRepository, ApprovalsRepository>();
            builder.Services.AddScoped<IGroupMembersRepository, GroupMembersRepository>();
            builder.Services.AddScoped<IGroupsRepository, GroupsRepository>();
            builder.Services.AddScoped<IInvitationsRepository, InvitationsRepository>();
            builder.Services.AddScoped<IJsonsRepository, JsonsRepository>();
            builder.Services.AddScoped<INodesRepository, NodesRepository>();
            builder.Services.AddScoped<IParentsRepository, ParentsRepository>();
            builder.Services.AddScoped<IPermissionsRepository, PermissionsRepository>();
            builder.Services.AddScoped<IOwnersRepository, OwnersRepository>();
            builder.Services.AddScoped<Contracts.Private.ISealsRepository, Repositories.Private.SealsRepository>();
            builder.Services.AddScoped<IVersionsRepository, VersionsRepository>();

            // Add Public repository and unit of work
            builder.Services.AddScoped<IBlockchainsRepository, BlockchainsRepository>();
            builder.Services.AddScoped<IDomainsRepository, DomainsRepository>();
            builder.Services.AddScoped<IEmailsRepository, EmailsRepository>();
            builder.Services.AddScoped<IInvitesRepository, InvitesRepository>();
            builder.Services.AddScoped<IOrgsRepository, OrgsRepository>();
            builder.Services.AddScoped<IBlockchainsRepository, BlockchainsRepository>();
            builder.Services.AddScoped<Contracts.Public.ISealsRepository, Repositories.Public.SealsRepository>();
            builder.Services.AddScoped<IPublicUsersRepository, PublicUsersRepository>();


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

            Servers.Instance.ImportOrgBlockchains();
            Servers.Instance.CreateRequiredFolders();

            if (!Servers.Instance.LoadSettings())
            {
                Console.WriteLine("can't found settings file!");
            }

            app.Run();
        }
    }
}