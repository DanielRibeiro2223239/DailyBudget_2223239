using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DailyBudgetWPF.Data.Services
{
    /// <summary>
    /// Serviço para comunicação com a Web API.
    /// Centraliza as chamadas HTTP e a gestão do token JWT para autenticação.
    /// </summary>
    public static class ApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5000/api/") };
        private static string? _token;

        public static void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        public static async Task<T?> GetAsync<T>(string endpoint)
        {
            try { return await _httpClient.GetFromJsonAsync<T>(endpoint); }
            catch { return default; }
        }

        public static async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
        {
            return await _httpClient.PostAsJsonAsync(endpoint, data);
        }

        public static async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
        {
            return await _httpClient.PutAsJsonAsync(endpoint, data);
        }

        public static async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            return await _httpClient.DeleteAsync(endpoint);
        }
    }
}
