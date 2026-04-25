using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using System.Security.Claims;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Cliente")]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SolicitudesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(SolicitudesFilterViewModel filter)
    {
        // Validaciones Server-Side
        if (filter.MontoMin.HasValue && filter.MontoMin < 0)
        {
            ModelState.AddModelError("MontoMin", "El monto mínimo no puede ser negativo.");
        }
        if (filter.MontoMax.HasValue && filter.MontoMax < 0)
        {
            ModelState.AddModelError("MontoMax", "El monto máximo no puede ser negativo.");
        }
        if (filter.FechaInicio.HasValue && filter.FechaFin.HasValue && filter.FechaInicio > filter.FechaFin)
        {
            ModelState.AddModelError("FechaFin", "La fecha de inicio no puede ser mayor a la fecha de fin.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var query = _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Cliente.UsuarioId == userId)
            .AsQueryable();

        if (ModelState.IsValid)
        {
            if (filter.Estado.HasValue)
            {
                query = query.Where(s => s.Estado == filter.Estado.Value);
            }
            if (filter.MontoMin.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado >= filter.MontoMin.Value);
            }
            if (filter.MontoMax.HasValue)
            {
                query = query.Where(s => s.MontoSolicitado <= filter.MontoMax.Value);
            }
            if (filter.FechaInicio.HasValue)
            {
                query = query.Where(s => s.FechaSolicitud >= filter.FechaInicio.Value);
            }
            if (filter.FechaFin.HasValue)
            {
                // Incluir todo el dia hasta las 23:59:59
                var fechaFinReal = filter.FechaFin.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.FechaSolicitud <= fechaFinReal);
            }
        }

        filter.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
        return View(filter);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id && s.Cliente.UsuarioId == userId);

        if (solicitud == null)
        {
            return NotFound();
        }

        return View(solicitud);
    }
}
