using AbarrotesPOS.Shared;
using AbarrotesPOS.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AbarrotesPOS.Data.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IMemoryCache _cache;

    // Prefijo para invalidar el caché fácilmente al guardar/eliminar
    private const string CachePrefix = "productos_search_";

    public ProductoRepository(IDbContextFactory<AppDbContext> contextFactory, IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    // ─── Paginación con búsqueda ─────────────────────────────────────────────

    public async Task<PagedResult<Producto>> GetProductosPaginados(
        int page, int pageSize, string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Proveedor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Nombre.Contains(search) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(search)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Producto>(items, total, page, pageSize);
    }

    // ─── Búsqueda rápida para PuntoDeVenta ───────────────────────────────────

    public async Task<List<ProductoDto>> SearchAsync(string term, int take = 10)
    {
        // Si el término está vacío no consultamos nada
        if (string.IsNullOrWhiteSpace(term))
            return new List<ProductoDto>();

        term = term.Trim();

        // Importante: no usar caché aquí. En ventas se necesita ver stock/precio actual
        // y evitar que varias búsquedas rápidas se queden atoradas con datos viejos.
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Productos
            .AsNoTracking()
            .Where(p =>
                p.Nombre.Contains(term) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(term)))
            .OrderBy(p => p.Nombre)
            .Take(take)
            .Select(p => new ProductoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                CodigoBarras = p.CodigoBarras,
                PrecioVenta = p.PrecioVenta,
                Stock = p.Stock,
                CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                ProveedorNombre = p.Proveedor != null ? p.Proveedor.Nombre : string.Empty
            })
            .ToListAsync();
    }

    // ─── Métodos auxiliares ──────────────────────────────────────────────────

    public async Task<Producto?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Proveedor)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Producto>> GetBajoStockAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.Stock <= p.StockMinimo)
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }

    /// <summary>
    /// Limpia el caché de búsqueda. Llamar después de guardar o eliminar un producto.
    /// </summary>
    public void InvalidarCache()
    {
        // IMemoryCache no tiene "limpiar por prefijo" nativo,
        // así que usamos un token de cancelación compartido.
        // La forma más simple es registrar las claves al crearlas:
        if (_cache is MemoryCache memCache)
            memCache.Compact(1.0); // En desarrollo está bien; en producción usar IMemoryCacheManager
    }
}