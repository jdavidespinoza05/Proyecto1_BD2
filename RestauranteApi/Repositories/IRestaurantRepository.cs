/*
Es el contrato que define cómo interactúa la base de datos con 
la información principal de los restaurantes o sucursales.
Su lista de tareas es súper directa: solo pide poder listar todos 
los restaurantes (GetAll) y registrar uno nuevo (Create).
Fíjate que no tiene métodos para modificar ni borrar. 

Esto nos dice que, al menos desde el alcance de esta API, una vez que se da 
de alta un restaurante, se queda fijo. 
Dato extra: el método "GetAllAsync" es justamente el que llama 
el RestaurantsController antes de guardar toda la lista en la caché 
que vimos hace un rato.
*/

using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IRestaurantRepository
    {
        Task<IEnumerable<Restaurant>> GetAllAsync();
        Task<Restaurant> CreateAsync(Restaurant restaurant);
    }
}