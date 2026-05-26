/*
Se encarga de manejar el login y el registro de usuarios en la API.

Básicamente funciona como un puente: se conecta con Keycloak para 
validar credenciales y generar los tokens de seguridad, delegando 
esa responsabilidad.

En el caso del registro, primero crea la cuenta en Keycloak y, si el 
proceso es exitoso, guarda los datos básicos del perfil en nuestra base 
de datos local utilizando IUsuarioRepository (para mantener la lógica 
de base de datos separada del controlador).
*/

using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RestaurantesApi.Models; 
using RestaurantesApi.Repositories; 

namespace RestaurantesApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    private readonly IUsuarioRepository _repository; 
    
    private readonly string _keycloakClientSecret = "zK4YVM7kzzJmZEAv6OWEQZR9GLkL1AFM"; 

    public AuthController(IHttpClientFactory httpClientFactory, IUsuarioRepository repository)
    {
        _httpClientFactory = httpClientFactory;
        _repository = repository;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Username))
            return BadRequest(new { mensaje = "Hacen falta datos." });

        var client = _httpClientFactory.CreateClient();
        
        var keycloakData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "restaurantes-api"),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_secret", _keycloakClientSecret),
            new KeyValuePair<string, string>("username", request.Username),
            new KeyValuePair<string, string>("password", request.Password)
        });

        var response = await client.PostAsync("http://keycloak:8080/realms/RestaurantesRealm/protocol/openid-connect/token", keycloakData);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
            return Ok(responseContent); 

        return BadRequest(new { mensaje = "Keycloak rechazó la petición", error_real = responseContent });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var usuarioExistente = await _repository.GetByEmailAsync(request.Email);
        if (usuarioExistente != null) return BadRequest(new { mensaje = "El correo ya está registrado." });

        var client = _httpClientFactory.CreateClient();

        var tokenData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "restaurantes-api"),
            new KeyValuePair<string, string>("client_secret", _keycloakClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var tokenResponse = await client.PostAsync("http://keycloak:8080/realms/RestaurantesRealm/protocol/openid-connect/token", tokenData);
        if (!tokenResponse.IsSuccessStatusCode){
            var contenidoError = await tokenResponse.Content.ReadAsStringAsync();
            return BadRequest(new { 
                mensaje = "Keycloak rechazó la petición de la API", 
                error_tecnico = contenidoError,
                codigo_status = (int)tokenResponse.StatusCode 
            });
        }
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var adminToken = JsonSerializer.Deserialize<JsonElement>(tokenJson).GetProperty("access_token").GetString();

        var nuevoUsuarioKeycloak = new
        {
            username = request.Username,
            email = request.Email,
            emailVerified = true,
            firstName = request.Nombre,
            lastName = "Desconocido",
            enabled = true,
            credentials = new[] { new { type = "password", value = request.Password, temporary = false } }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(nuevoUsuarioKeycloak), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        var createKcResponse = await client.PostAsync("http://keycloak:8080/admin/realms/RestaurantesRealm/users", jsonContent);

        if (!createKcResponse.IsSuccessStatusCode)
        {
            var errorKc = await createKcResponse.Content.ReadAsStringAsync();
            return BadRequest(new { mensaje = "Error al crear en Keycloak.", detalle = errorKc });
        }

        var keycloakUserId = createKcResponse.Headers.Location?.Segments.Last();

        try 
        {
            var nuevoUsuarioDb = new Usuario 
            {
                Nombre = request.Nombre,
                Correo = request.Email,
                Rol = string.IsNullOrWhiteSpace(request.Rol) ? "cliente" : request.Rol.ToLower()
            };

            await _repository.CreateAsync(nuevoUsuarioDb);

            return Ok(new { 
                mensaje = "Usuario creado.", 
                id_keycloak = keycloakUserId,
                usuario_db = nuevoUsuarioDb.Nombre 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Se creó en Keycloak pero falló la BD local", error = ex.Message });
        }
    }
}

public class LoginRequest { public string Username { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }
public class RegisterRequest { public string Username { get; set; } = string.Empty; public string Nombre { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public string Rol { get; set; } = "Cliente"; }