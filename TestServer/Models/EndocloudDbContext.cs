using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TestServer.Models.Private;

namespace TestServer.Models
{
    public class EndocloudDbContext : DbContext
    {
        // options is coming from Program.cs
        public EndocloudDbContext(DbContextOptions<EndocloudDbContext> options) : base(options)
        {
        }

        public DbSet<Approval> Approvals { get; set; }
        public DbSet<Audit> Audits { get; set; }
        public DbSet<Private.Attribute> Attributes { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Json> Jsons { get; set; }
        public DbSet<Node> Nodes { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Seal> Seals { get; set; }
        public DbSet<Private.Version> Versions { get; set; }

    }

    public class EndocloudDbContextFactory : IDesignTimeDbContextFactory<EndocloudDbContext>
    {
        public EndocloudDbContext CreateDbContext(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<EndocloudDbContext>();
            var conn = config.GetConnectionString("EndocloudDbConnectionString");
            optionsBuilder.UseSqlServer(conn);
            //optionsBuilder.UseSqlite(conn);

            return new EndocloudDbContext(optionsBuilder.Options);

        }
    }
}
