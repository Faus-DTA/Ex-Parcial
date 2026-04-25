using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Cliente")]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public SolicitudesController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
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
        string cacheKey = $"solicitudes_{userId}";
        
        List<SolicitudCredito> listaSolicitudes;
        var cacheData = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(cacheData))
        {
            listaSolicitudes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Cliente.UsuarioId == userId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };

            var json = JsonConvert.SerializeObject(listaSolicitudes, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            await _cache.SetStringAsync(cacheKey, json, cacheOptions);
        }
        else
        {
            listaSolicitudes = JsonConvert.DeserializeObject<List<SolicitudCredito>>(cacheData) ?? new List<SolicitudCredito>();
        }

        var query = listaSolicitudes.AsEnumerable();

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

        filter.Solicitudes = query.ToList();
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

        // Guardar en sesión
        HttpContext.Session.SetString("UltimaSolicitudId", solicitud.Id.ToString());
        HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString("C"));

        return View(solicitud);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSolicitudViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (cliente == null)
        {
            return NotFound();
        }

        // Regla: Cliente debe estar activo
        if (!cliente.Activo)
        {
            ModelState.AddModelError("", "Tu cuenta de cliente no está activa. No puedes solicitar créditos.");
            return View(model);
        }

        // Regla: No permitir más de una solicitud Pendiente por cliente.
        var tienePendiente = await _context.SolicitudesCredito
            .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);
        
        if (tienePendiente)
        {
            ModelState.AddModelError("", "Ya tienes una solicitud en estado Pendiente. Debes esperar a que sea evaluada.");
            return View(model);
        }

        // Regla: El monto solicitado no puede superar 10 veces los ingresos mensuales.
        if (model.MontoSolicitado > (cliente.IngresosMensuales * 10))
        {
            ModelState.AddModelError("MontoSolicitado", $"El monto no puede superar 10 veces tus ingresos mensuales (Máx: {(cliente.IngresosMensuales * 10):C}).");
            return View(model);
        }

        var nuevaSolicitud = new SolicitudCredito
        {
            ClienteId = cliente.Id,
            MontoSolicitado = model.MontoSolicitado,
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoSolicitud.Pendiente
        };

        _context.SolicitudesCredito.Add(nuevaSolicitud);
        await _context.SaveChangesAsync();

        // Invalidar cache
        await _cache.RemoveAsync($"solicitudes_{userId}");

        TempData["Success"] = "Solicitud creada con éxito. Pronto será evaluada.";
        return RedirectToAction(nameof(Index));
    }
}
