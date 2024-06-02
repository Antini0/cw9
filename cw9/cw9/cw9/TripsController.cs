using cw9.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cw9;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly Cw9Context _context;
    public TripsController(Cw9Context context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await _context.Trips.Select(e => new
        {
            Name = e.Name,
            Countries = e.IdCountries.Select(c => new
            {
                Name = c.Name
            })
        }).ToListAsync();
        
        return Ok(trips);
    }
}