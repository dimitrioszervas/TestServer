﻿using TestServer.Models.Public;
using TestServer.Contracts;

namespace TestServer.Contracts.Public
{
    public interface IOrgsRepository : IGenericRepository<TestServer.Models.Public.Org>
    {
    }
}
