using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TestServer.Models.Public;

namespace TestServer.Models
{
    public class PublicDbContext : DbContext
    {
        // options is coming from Program.cs
        public PublicDbContext(DbContextOptions<PublicDbContext> options) : base(options)
        {
        }

        public DbSet<Blockchain> Blockchains { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<Invite> Invites { get; set; }
        public DbSet<Org> Orgs { get; set; }
        public DbSet<Seal> Seals { get; set; }
        public DbSet<SealTransaction> SealTransactions { get; set; }
        public DbSet<PublicUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }


    public class PublicDbContextFactory : IDesignTimeDbContextFactory<PublicDbContext>
    {
        public PublicDbContext CreateDbContext(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<PublicDbContext>();
            var conn = config.GetConnectionString("PublicDbConnectionString");
            optionsBuilder.UseSqlServer(conn);
            //optionsBuilder.UseSqlite(conn);

            return new PublicDbContext(optionsBuilder.Options);

        }
    }
}
