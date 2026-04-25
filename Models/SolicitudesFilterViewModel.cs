namespace PlataformaCreditos.Models;

public class SolicitudesFilterViewModel
{
    public EstadoSolicitud? Estado { get; set; }
    public decimal? MontoMin { get; set; }
    public decimal? MontoMax { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    
    public IEnumerable<SolicitudCredito> Solicitudes { get; set; } = new List<SolicitudCredito>();
}
