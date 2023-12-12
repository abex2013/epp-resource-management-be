﻿using Excellerent.ClientManagement.Domain.Entities;
using Excellerent.ClientManagement.Domain.Interfaces.ServiceInterface;
using Excellerent.ClientManagement.Domain.Models;
using Excellerent.SharedModules.Seed;
using Excellerent.SharedModules.Services;

namespace Excellerent.ClientManagement.Domain.Services
{
    public class ClientContactService : CRUD<ClientContactEntity, ClientContact>, IClientContactService
    {
        public ClientContactService(IAsyncRepository<ClientContact> repository) : base(repository)
        {
        }
    }
}