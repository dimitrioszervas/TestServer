using TestServer.Models.Public;
using TestServer.Contracts;

namespace TestServer.Contracts.Public
{
    public interface IEmailsRepository : IGenericRepository<Email>
    {
        Task<Email> GetByEmailAddressAsync(string emailAddress);
    }
}
