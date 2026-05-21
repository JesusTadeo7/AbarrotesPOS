using System.ComponentModel.DataAnnotations.Schema;

namespace AbarrotesPOS.Shared;

/// <summary>
/// Representa una transacción de venta única (un ticket o recibo).
/// </summary>
public class Venta
{
    public int Id { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }

    // Foreign Key para el empleado que realizó la venta
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    public int? ClienteId { get; set; }

    [ForeignKey("ClienteId")]
    public Cliente? Cliente { get; set; }

    // Propiedad de navegación para los detalles de la venta
    public List<VentaDetalle> Detalles { get; set; } = new();
}


