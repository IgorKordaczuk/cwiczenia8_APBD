using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        //Zwracamy wycieczki z listą krajów
        string command =  @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetOrdinal("IdTrip");
                    
                    var trip = new TripDTO()
                    {
                        Id = reader.GetInt32(idOrdinal),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5),
                        Countries = reader.IsDBNull(6) ? null : new List<CountryDTO>()
                    };
                    
                    // Add country if present
                    if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                    {
                        trip.Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(reader.GetOrdinal("CountryName"))
                        });
                    }

                    trips.Add(trip);

                }
            }
        }
        

        return trips;
    }
}