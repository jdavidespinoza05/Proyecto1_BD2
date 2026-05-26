/*
 Se encarga de las búsquedas de productos en el sistema.
 En lugar de buscar directamente en la base de datos con un repositorio, 
 este controlador utiliza un servicio (ISearchService) que se comunica 
 con ElasticSearch para hacer búsquedas de texto mucho más rápidas y precisas.
 
 Tiene endpoints para buscar por palabra clave general o filtrando dentro 
 de una categoría específica.
 Incluye un método especial (reindex) que sirve para borrar y volver a 
 cargar un set de datos de prueba directamente en el índice de ElasticSearch. 
 
 Esto resulta súper útil para probar que las búsquedas funcionen bien sin 
 tener que meter datos a mano.
 */

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

        [HttpPost("reindex")]
        public async Task<IActionResult> Reindex()
        {
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