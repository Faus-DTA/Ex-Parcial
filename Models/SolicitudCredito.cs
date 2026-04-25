using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaCreditos.Models;

public class SolicitudCredito
{
    public int Id { get; set; }
    
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0.")]
    public decimal MontoSolicitado { get; set; }
    
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    
    public string? MotivoRechazo { get; set; }

    // Regla de Negocio
    public bool PuedeSerAprobada()
    {
        if (Cliente == null) return false;
        return MontoSolicitado <= (Cliente.IngresosMensuales * 5);
    }
}
