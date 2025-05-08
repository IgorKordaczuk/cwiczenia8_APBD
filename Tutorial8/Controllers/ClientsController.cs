using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientsService _clientsService;

        public ClientsController(IClientsService clientsService)
        {
            _clientsService = clientsService;
        }

        /*
        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _clientsService.GetTrips();
            return Ok(trips);
        }
        */
        
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetTrip(int id)
        {
            var trips = await _clientsService.GetTrips(id);
            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> PostClient(CreateClientDTO createClientDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int newClientId = await _clientsService.PostClient(createClientDto);
            return Ok(newClientId);
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientOnTrip(int id, int tripId)
        {
            string result = await _clientsService.RegisterClientOnTrip(id, tripId);

            return result switch
            {
                "ClientNotFound" => NotFound("Client not found."),
                "TripNotFound" => NotFound("Trip not found."),
                "TripFull" => BadRequest("Maximum number of participants reached."),
                "AlreadyRegistered" => Conflict("Client is already registered."),
                "Success" => Ok("Client successfully registered for the trip."),
            };
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> PutClient(int id, int tripId)
        {
            bool result = await _clientsService.DeleteClientTrip(id, tripId);

            return result switch
            {
                false => NotFound("Client is not registered on this trip."),
                true => Ok("Client successfully removed from the trip."),
            };
        }
    }
}