using cw9.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using cw9.DTO;
using cw9.Models;

namespace cw9;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly Cw9Context _context;
    private const int DefaultPageSize = 10;

    public TripsController(Cw9Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = DefaultPageSize;

        var trips = await _context.Trips
            .OrderByDescending(t => t.DateFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                Name = e.Name,
                Countries = e.IdCountries.Select(c => new
                {
                    Name = c.Name
                })
            })
            .ToListAsync();

        return Ok(trips);
    }
    
    
    
    [HttpPost("{idTrip}/clients")]
        public async Task<IActionResult> AssignClientToTrip(int idTrip, [FromBody] ClientDto clientDto)
        {
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.Pesel == clientDto.Pesel);
            if (existingClient != null)
            {
                return BadRequest(new { message = "Klient o podanym numerze PESEL już istnieje." });
            }

            var existingClientTrip = await _context.ClientTrips
                .FirstOrDefaultAsync(ct => ct.IdTrip == idTrip && ct.IdClientNavigation.Pesel == clientDto.Pesel);
            if (existingClientTrip != null)
            {
                return BadRequest(new { message = "Klient o podanym numerze PESEL jest już zapisany na tę wycieczkę." });
            }

            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.IdTrip == idTrip);
            if (trip == null)
            {
                return NotFound(new { message = "Wycieczka nie istnieje." });
            }
            if (trip.DateFrom <= DateTime.Now)
            {
                return BadRequest(new { message = "Nie można zapisać się na wycieczkę, która już się odbyła." });
            }

            var newClient = new Client
            {
                FirstName = clientDto.FirstName,
                LastName = clientDto.LastName,
                Email = clientDto.Email,
                Telephone = clientDto.Telephone,
                Pesel = clientDto.Pesel
            };

            _context.Clients.Add(newClient);
            await _context.SaveChangesAsync();

            var clientTrip = new ClientTrip
            {
                IdClient = newClient.IdClient,
                IdTrip = idTrip,
                RegisteredAt = DateTime.Now,
                PaymentDate = clientDto.PaymentDate
            };

            _context.ClientTrips.Add(clientTrip);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Klient został pomyślnie zapisany na wycieczkę." });
        }
}