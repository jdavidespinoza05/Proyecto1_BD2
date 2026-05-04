using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using RestaurantesApi.Models;

namespace RestaurantesApi.Services
{
    public class ElasticSearchService : ISearchService
    {
        private readonly ElasticsearchClient _client;
        private const string IndexName = "productos"; 

        public ElasticSearchService(ElasticsearchClient client)
        {
            _client = client;
        }

        public async Task<bool> ReindexAllAsync(IEnumerable<ProductoBusqueda> productos)
        {
            // REGLA DE NEGOCIO: Manejo especial para productos sin descripción
            foreach (var p in productos)
            {
                if (string.IsNullOrWhiteSpace(p.Descripcion))
                {
                    p.Descripcion = "Sin descripción disponible";
                }
            }

            // 1. Si el índice ya existe, lo borramos para empezar desde cero
            var existsResponse = await _client.Indices.ExistsAsync(IndexName);
            
            if (existsResponse.IsValidResponse) 
            {
                await _client.Indices.DeleteAsync(IndexName);
            }

            // 2. Insertamos todos los productos de golpe (Bulk)
            var bulkResponse = await _client.BulkAsync(b => b
                .Index(IndexName)
                // Usamos el ID del objeto para evitar duplicados y facilitar el mapeo
                .IndexMany(productos, (descriptor, producto) => descriptor.Id(producto.Id)) 
                // Refresh.WaitFor obliga a ElasticSearch a confirmar que el dato es buscable
                .Refresh(Refresh.WaitFor) 
            );

            // 3. Verificación de errores compatible con la versión 8.13
            if (!bulkResponse.IsValidResponse)
            {
                Console.WriteLine("ERRORES AL GUARDAR EN ELASTICSEARCH: " + bulkResponse.DebugInformation);
                return false;
            }

            return true;
        }

        public async Task<IEnumerable<ProductoBusqueda>> SearchProductsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<ProductoBusqueda>();

            var response = await _client.SearchAsync<ProductoBusqueda>(s => s
                .Indices(IndexName)
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(keyword)
                        .Fields(new[] { "nombre", "descripcion" }) 
                        .Fuzziness(new Fuzziness("AUTO"))
                    )
                )
            );

            if (!response.IsValidResponse)
            {
                Console.WriteLine("ERROR EN BÚSQUEDA GENERAL: " + response.DebugInformation);
            }
            Console.WriteLine("DEBUG INFO ELASTIC: \n" + response.DebugInformation);
            return response.Documents;
        }

        public async Task<IEnumerable<ProductoBusqueda>> SearchByCategoryAsync(string category, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<ProductoBusqueda>();

            var response = await _client.SearchAsync<ProductoBusqueda>(s => s
                .Indices(IndexName)
                .Query(q => q
                    .Bool(b => b
                        // MUST: La palabra clave debe coincidir en nombre o descripción
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(keyword)
                                .Fields(new[] { "nombre", "descripcion" })
                                .Fuzziness(new Fuzziness("AUTO"))
                            )
                        )
                        // FILTER: Restringe los resultados únicamente a la categoría indicada
                        .Filter(f => f
                            .Match(m => m
                                .Field(p => p.Categoria)
                                .Query(category)
                            )
                        )
                    )
                )
            );

            if (!response.IsValidResponse)
            {
                Console.WriteLine("ERROR EN BÚSQUEDA FILTRADA: " + response.DebugInformation);
            }
            Console.WriteLine("DEBUG INFO ELASTIC: \n" + response.DebugInformation);
            return response.Documents;
        }
    }
}