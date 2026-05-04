using RestaurantesApi.Models;

namespace RestaurantesApi.Services
{
    public interface ISearchService
    {
        // Reconstruye el índice completo (Endpoint: /search/reindex)
        Task<bool> ReindexAllAsync(IEnumerable<ProductoBusqueda> productos);
        
        // Búsqueda general (Endpoint: /search/products)
        Task<IEnumerable<ProductoBusqueda>> SearchProductsAsync(string keyword);
        
        // Búsqueda filtrada por categoría (Endpoint: /search/products/category/:categoria)
        Task<IEnumerable<ProductoBusqueda>> SearchByCategoryAsync(string category, string keyword);
    }
}