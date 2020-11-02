using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using backend.Models;
using backend.Utils;

namespace backend.Controllers {

  [Authorize]
  [ApiController]
  [Route("[controller]")]
  public class AgentsController : ControllerBase {
    private readonly rapidesqlContext _context;
    private readonly string _routeKey;

    public AgentsController(rapidesqlContext context, IConfiguration configuration) {
      _context = context;
      _routeKey = configuration.GetValue<string>("AppSettings:RouteKey");
    }

    // GET: api/Agents
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Agents>>> GetAgents() {
      return await _context.Agents.ToListAsync();
    }

    // GET: api/Agents/5
    //[HttpGet("{id}")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Agents>> GetAgents(int id) {
      try {
        var agents = await _context.Agents.FindAsync(id);

        if (agents == null) {
          return NotFound();
        }

        return Ok(agents);
      }
      catch(Exception ex) {
        return Content(ex.Message);
      }
    }

    // PUT: api/Agents/5
    // To protect from overposting attacks, enable the specific properties you want to bind to, for
    // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAgents(int id, Agents agents) {
      if (id != agents.IdAgent) {
        return BadRequest();
      }

      _context.Entry(agents).State = EntityState.Modified;

      try {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException) {
        if (!AgentsExists(id)) {
          return NotFound();
        }
        else {
          throw;
        }
      }

      return NoContent();
    }

    // POST: api/Agents
    // To protect from overposting attacks, enable the specific properties you want to bind to, for
    // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
    [HttpPost]
    public async Task<ActionResult<Agents>> PostAgents(Agents agents) {
      _context.Agents.Add(agents);
      await _context.SaveChangesAsync();

      return CreatedAtAction("GetAgents", new { id = agents.IdAgent }, agents);
    }

    // DELETE: api/Agents/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<Agents>> DeleteAgents(int id) {
      var agents = await _context.Agents.FindAsync(id);
      if (agents == null) {
        return NotFound();
      }

      _context.Agents.Remove(agents);
      await _context.SaveChangesAsync();

      return agents;
    }

    private bool AgentsExists(int id) {
      return _context.Agents.Any(e => e.IdAgent == id);
    }
  }
}
