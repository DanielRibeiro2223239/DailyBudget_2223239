using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração da autenticação JWT (JSON Web Token)
// O JWT permite autenticar os pedidos à API sem usar sessões no servidor
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,              // Verifica quem emitiu o token
            ValidateAudience = true,            // Verifica para quem o token foi emitido
            ValidateLifetime = true,            // Verifica se o token não expirou
            ValidateIssuerSigningKey = true,    // Verifica a assinatura do token
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Adiciona suporte para Controllers (padrão MVC da API)
builder.Services.AddControllers();

// Configuração do Swagger para documentação automática da API
// O Swagger gera uma interface web interativa onde se podem testar os endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DailyBudget API",
        Version = "v1",
        Description = "API REST para gestão de finanças pessoais — PAP 2223239 Daniel Ribeiro"
    });

    // Configuração para o Swagger aceitar tokens JWT no header Authorization
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduza o token JWT obtido no endpoint /api/auth/login"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuração de CORS para permitir que a app WPF comunique com a API
// CORS (Cross-Origin Resource Sharing) controla que origens podem aceder à API
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirWPF", policy =>
    {
        policy.AllowAnyOrigin()     // Permite qualquer origem (WPF, browser, etc.)
              .AllowAnyMethod()     // Permite GET, POST, PUT, DELETE
              .AllowAnyHeader();    // Permite qualquer header (incluindo Authorization)
    });
});

var app = builder.Build();

// Ativa o Swagger apenas em ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DailyBudget API v1");
        c.RoutePrefix = string.Empty; // Swagger acessível na raiz (localhost:5000/)
    });
}

app.UseCors("PermitirWPF");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// A API corre na porta 5000 (HTTP) e 5001 (HTTPS)
app.Run();
