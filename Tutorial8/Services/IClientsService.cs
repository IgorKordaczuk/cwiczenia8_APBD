using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<List<TripDTO>> GetTrips(int IdClient);
    Task<int> PostClient(CreateClientDTO createClientDto);
    Task<string> RegisterClientOnTrip(int IdClient, int IdTrip);
    Task<bool> DeleteClientTrip(int IdClient, int IdTrip);
};