using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Services;

namespace RestaurantesApi.Controllers
{
    [ApiController]
    [Route("search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        // 1. Endpoint para búsqueda general
        // GET /search/products?keyword=hamburguesa
        [HttpGet("products")]
        public async Task<IActionResult> SearchProducts([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest("Debes enviar una palabra clave en el parámetro 'keyword'.");
            }

            var resultados = await _searchService.SearchProductsAsync(keyword);
            return Ok(resultados);
        }

        // 2. Endpoint para búsqueda filtrada por categoría
        // GET /search/products/category/Bebidas?keyword=cola
        [HttpGet("products/category/{categoria}")]
        public async Task<IActionResult> SearchByCategory(string categoria, [FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest("Debes enviar una palabra clave en el parámetro 'keyword'.");
            }

            var resultados = await _searchService.SearchByCategoryAsync(categoria, keyword);
            return Ok(resultados);
        }

        // 3. Endpoint para reconstruir el índice y aplicar reglas de negocio
        // POST /search/reindex
        [HttpPost("reindex")]
        public async Task<IActionResult> Reindex()
        {
            // Datos de prueba para verificar que ElasticSearch funciona.
            // Fíjate cómo las "Papas Fritas" no tienen descripción para probar tu regla de negocio.
            var datosDePrueba = new List<ProductoBusqueda>
            {
                new ProductoBusqueda { Id = 1, Nombre = "Hamburguesa Clásica", Descripcion = "Carne, queso, tomate", Precio = 5000, Categoria = "Platos Fuertes" },
                new ProductoBusqueda { Id = 2, Nombre = "Papas Fritas", Descripcion = "", Precio = 2000, Categoria = "Acompañamientos" }, 
                new ProductoBusqueda { Id = 3, Nombre = "Refresco de Cola", Descripcion = "Bebida azucarada bien fría", Precio = 1500, Categoria = "Bebidas" },
                new ProductoBusqueda { Id = 4, Nombre = "Pizza Pepperoni", Descripcion = "Queso mozzarella y pepperoni", Precio = 8000, Categoria = "Platos Fuertes" }
            };

            var exito = await _searchService.ReindexAllAsync(datosDePrueba);

            if (exito)
            {
                return Ok(new { Mensaje = "Índice de ElasticSearch reconstruido con éxito. Reglas de negocio aplicadas." });
            }
            
            return StatusCode(500, new { Mensaje = "Error interno al comunicarse con ElasticSearch." });
        }
    }
}