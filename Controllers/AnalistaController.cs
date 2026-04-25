using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AnalistaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var solicitudes = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(s => s.FechaSolicitud)
            .ToListAsync();

        return View(solicitudes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "La solicitud ya ha sido procesada.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.MontoSolicitado > (solicitud.Cliente.IngresosMensuales * 5))
        {
            TempData["Error"] = "No se puede aprobar. El monto excede 5 veces los ingresos mensuales del cliente.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Solicitud #{solicitud.Id} aprobada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Rechazar(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "La solicitud ya ha sido procesada.";
            return RedirectToAction(nameof(Index));
        }

        return View(solicitud);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string motivoRechazo)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "La solicitud ya ha sido procesada.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(motivoRechazo))
        {
            ModelState.AddModelError("motivoRechazo", "El motivo de rechazo es obligatorio.");
            return View(solicitud);
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = motivoRechazo;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Solicitud #{solicitud.Id} rechazada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
