/*
 * IMenuRepository
 * Esta es la interfaz o "contrato" para la gestión de menús.
 * Aquí no hay lógica de base de datos, solo la lista de tareas obligatorias 
 * que cualquier repositorio (ya sea de Postgres o de Mongo) debe saber hacer.
 * Define las operaciones básicas (CRUD): crear, buscar por ID, actualizar 
 * y borrar, además de métodos útiles para validar si un restaurante o un 
 * platillo existen.
 * Es la pieza clave que permite al controlador funcionar a ciegas, 
 * sin importarle si los datos se están guardando en SQL o NoSQL.
 */

using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IMenuRepository
    {
        Task<bool> RestaurantExistsAsync(int restaurantId);
        Task<Menu> CreateAsync(Menu menu);
        Task<Menu?> GetByIdAsync(int id);
        Task UpdateAsync(Menu menu);
        Task<bool> MenuExistsAsync(int id);
        Task DeleteAsync(int id);
    }
}