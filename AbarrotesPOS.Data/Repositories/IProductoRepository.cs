using AbarrotesPOS.Shared;
using AbarrotesPOS.Shared.DTOs;

namespace AbarrotesPOS.Data.Repositories;

public interface IProductoRepository
{
    /// <summary>Lista paginada con filtro opcional de búsqueda.</summary>
    Task<PagedResult<Producto>> GetProductosPaginados(int page, int pageSize, string? search = null);

    /// <summary>Búsqueda rápida con caché. Usada en el buscador del PuntoDeVenta.</summary>
    Task<List<ProductoDto>> SearchAsync(string term, int take = 10);

    Task<Producto?> GetByIdAsync(int id);
    Task<List<Producto>> GetBajoStockAsync();
    void InvalidarCache();
}