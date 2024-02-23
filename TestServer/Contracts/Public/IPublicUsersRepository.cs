using TestServer.Models.Public;
using TestServer.Contracts;

namespace TestServer.Contracts.Public
{
    public interface IPublicUsersRepository : IGenericRepository<PublicUser>
    {
    }
}
