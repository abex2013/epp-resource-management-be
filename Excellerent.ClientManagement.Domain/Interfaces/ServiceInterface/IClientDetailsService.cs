﻿using Excellerent.ClientManagement.Domain.Entities;
using Excellerent.ClientManagement.Domain.Models;
using Excellerent.SharedModules.DTO;
using Excellerent.SharedModules.Interface.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Excellerent.ClientManagement.Domain.Interfaces.ServiceInterface
{
    public interface IClientDetailsService : ICRUD<ClientDetailsEntity, ClientDetails>
    {
        Task<IEnumerable<ClientDetailsEntity>> GetClientByName(string clientName);

        Task<ResponseDTO> GetClientFullData();

        Task<PredicatedResponseDTO> GetPaginatedClient(Guid? id, string searchKey, int? pageIndex, int? pageSize);

        public Task<ResponseDTO> AddNewClient(ClientDetailsEntity client);
    }
}