using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DailyBudgetWPF.Modelos;

namespace DailyBudgetWPF.Dados.Servicos
{
    public class ServicoApi
    {
        private readonly HttpClient _client;

        public ServicoApi()
        {
            _client = new HttpClient { BaseAddress = new Uri("https://localhost:7150/api/") };
        }

        public async Task<Utilizador?> LoginAsync(string email, string password)
        {
            var response = await _client.PostAsJsonAsync("auth/login", new { Email = email, Password = password });
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Utilizador>() : null;
        }

        public async Task<bool> RegisterAsync(string nome, string email, string password)
        {
            var response = await _client.PostAsJsonAsync("auth/register", new { Nome = nome, Email = email, Password = password });
            return response.IsSuccessStatusCode;
        }
    }
}
