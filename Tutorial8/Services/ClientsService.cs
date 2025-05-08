using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

    public async Task<List<TripDTO>> GetTrips(int IdClient)
    {
        var tripsDict = new Dictionary<int, TripDTO>();

        //Zapytanie zwracające Tripy Klientów z listami krajów
        string command = @"
        SELECT 
            t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
            ct.Name AS CountryName,
            clt.RegisteredAt, clt.PaymentDate
        FROM Client c
        JOIN Client_Trip clt ON c.IdClient = clt.IdClient
        JOIN Trip t ON clt.IdTrip = t.IdTrip
        LEFT JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
        LEFT JOIN Country ct ON ctr.IdCountry = ct.IdCountry
        WHERE c.IdClient = @ClientId";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@ClientId", IdClient);
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                    if (!tripsDict.TryGetValue(tripId, out var trip))
                    {
                        trip = new TripDTO
                        {
                            Id = tripId,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = new List<CountryDTO>(),
                            RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                            PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("PaymentDate"))
                        };

                        tripsDict[tripId] = trip;
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                    {
                        trip.Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(reader.GetOrdinal("CountryName"))
                        });
                    }
                }
            }
        }

        return tripsDict.Values.ToList();
    }

    public async Task<int> PostClient(CreateClientDTO createClientDto)
    {
        //
        string command = @"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
        
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(command, conn);

            cmd.Parameters.AddWithValue("@FirstName", createClientDto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", createClientDto.LastName);
            cmd.Parameters.AddWithValue("@Email", createClientDto.Email);
            cmd.Parameters.AddWithValue("@Telephone", createClientDto.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", createClientDto.Pesel);

            await conn.OpenAsync();
            var resId = (int)await cmd.ExecuteScalarAsync();
            return resId;
    }

    public async Task<string> RegisterClientOnTrip(int IdClient, int IdTrip)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Sprawdzamny czy klient istnieje
        {
            string command = "SELECT 1 FROM Client WHERE IdClient = @ClientId";
            
            using (var cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", IdClient);
                var exists = await cmd.ExecuteScalarAsync();
                if (exists == null) return "ClientNotFound";
            }
        }

        // Sprawdzanie czy Trip istnieje i ustalenie maxPeople
        int maxPeople = 0;
        {
            string command = "SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId";
            
            using (var cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", IdTrip);
                var result = await cmd.ExecuteScalarAsync();
                if (result == null) return "TripNotFound";
                maxPeople = (int)result;
            }
        }

        // Zliczanie aktualnych uczestników
        {
            string command = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId";
            
            using (var cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", IdTrip);
                int count = (int)await cmd.ExecuteScalarAsync();
                if (count >= maxPeople) return "TripFull";
            }
        }

        // Sprawdzanie czy klient jest już zarejestrowany na wycieczkę
        {
            string command = "SELECT 1 FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId";
            
            using (var cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", IdClient);
                cmd.Parameters.AddWithValue("@TripId", IdTrip);
                var already = await cmd.ExecuteScalarAsync();
                if (already != null) return "AlreadyRegistered";
            }   
        }

        // Wstawiamy
        {
            string command =
                "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@ClientId, @TripId, @Now)";
                
            using (var cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", IdClient);
                cmd.Parameters.AddWithValue("@TripId", IdTrip);
                cmd.Parameters.AddWithValue("@Now", 1);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        return "Success";
    }

    public async Task<bool> DeleteClientTrip(int IdClient, int IdTrip)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Sprawdzamy czy rejestracja istnieje
        var checkCmd = new SqlCommand(
            "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection);
        checkCmd.Parameters.AddWithValue("@IdClient", IdClient);
        checkCmd.Parameters.AddWithValue("@IdTrip", IdTrip);

        var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

        if (!exists) return false;

        // Usuwamy rejestrację
        var deleteCmd = new SqlCommand(
            "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection);
        deleteCmd.Parameters.AddWithValue("@IdClient", IdClient);
        deleteCmd.Parameters.AddWithValue("@IdTrip", IdTrip);

        await deleteCmd.ExecuteNonQueryAsync();
        return true;
    }
}