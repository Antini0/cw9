using cw9.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace cw9;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly Cw9Context _context;

    public ClientsController(Cw9Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var totalClients = await _context.Clients.CountAsync();
        var allPages = (int)Math.Ceiling(totalClients / (double)pageSize);

        var clients = await _context.Clients
            .Include(c => c.ClientTrips)
            .ThenInclude(ct => ct.IdTripNavigation)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(client => new
            {
                client.IdClient,
                client.FirstName,
                client.LastName,
                client.Email,
                client.Telephone,
                client.Pesel,
                Trips = client.ClientTrips.Select(ct => new
                {
                    ct.IdTripNavigation.IdTrip,
                    ct.IdTripNavigation.Name,
                    ct.IdTripNavigation.Description,
                    ct.IdTripNavigation.DateFrom,
                    ct.IdTripNavigation.DateTo,
                    ct.IdTripNavigation.MaxPeople
                }).ToList()
            })
            .ToListAsync();

        var response = new
        {
            pageNum = page,
            pageSize = pageSize,
            allPages = allPages,
            clients = clients
        };

        return Ok(response);
    }

    [HttpDelete("{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var client = await _context.Clients
            .Include(c => c.ClientTrips)
            .FirstOrDefaultAsync(c => c.IdClient == idClient);
        
        if (client == null)
        {
            return NotFound(new { message = "Klient nie istnieje." });
        }

        if (client.ClientTrips.Any())
        {
            return BadRequest(new { message = "Klient ma przypisane wycieczki i nie może zostać usunięty." });
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}