using AutoMapper;
using TestServer.Contracts.Public;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Models.Public;
using TestServer.Repositories;

namespace TestServer.Repositories.Public
{
    public class EmailsRepository : PublicGenericRepository<Email>, IEmailsRepository
    {
        private readonly PublicDbContext _context;

        public EmailsRepository(PublicDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public async Task<Email> GetByEmailAddressAsync(string emailAddress)
        {
            var email = from e in _context.Emails
                        where e.EmailAddress == emailAddress
                        select e;

            return email.FirstOrDefault();
        }
    }
}
