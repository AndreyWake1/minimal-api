using MinimalApi.Dominio.Enuns;
using Microsoft.AspNetCore.Authorization;
using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Dominio.Entidades;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.ModelViews;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;


#region  Builder

var builder = WebApplication.CreateBuilder(args);
var Key = builder.Configuration.GetSection("Jwt").ToString();
if(string.IsNullOrEmpty(Key)) Key = "minimal_api";

builder.Services.AddControllers();
builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();
builder.Services.AddScoped<ICadastroServico, CadastroServico>();
builder.Services.AddDbContext<DbContexto>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("Sql")
));
builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme  = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key)),
        ValidateAudience = false,
        ValidateIssuer =  false,
    };
});
builder.Services.AddAuthorization();
// Adiciona Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(Options =>
{
    Options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token: Bearer{Seu Token}"
    });

    Options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
    
    
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

    var app = builder.Build();

    #endregion

#region Cadastro

    string GerarTokenJwt(Administrador administrador)
    {
        if (string.IsNullOrEmpty(Key)) return string.Empty;

        var securitykey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
        var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil.ToString()),
        new Claim(ClaimTypes.Role, administrador.Perfil)
    };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    app.MapPost("/Login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
    {
        var adm = administradorServico.Login(loginDTO);

        if (adm != null)
        {
            string token = GerarTokenJwt(adm);

            return Results.Ok(new AdministradorLogado
            {
                Email = adm.Email,
                Perfil = adm.Perfil,
                Token = token
            });
        }

        return Results.Unauthorized();

    }).AllowAnonymous().WithTags("User");

    app.MapPost("/Cadastro", (CadastroDTO dTO, ICadastroServico cadastroServico) =>
    {

        var validacao = new ErrosDeValidacao
        {
            Mensagens = new List<string>()
        };

        if (string.IsNullOrEmpty(dTO.Email))
            validacao.Mensagens.Add("Nao e possivel cadastrar administrador sem Email");

        if (string.IsNullOrEmpty(dTO.Senha))
            validacao.Mensagens.Add("Nao e possivel cadastrar administrador sem Senha");

        if (dTO.Perfil == null)
            validacao.Mensagens.Add("Nao e possivel cadastrar administrador sem Perfil");

        if (validacao.Mensagens.Count > 0)
            return Results.BadRequest(validacao);
        try
        {

            var cadastroCriado = cadastroServico.Incluir(dTO);
            return Results.Created($"/cadastro/{cadastroCriado.Id}", cadastroCriado);
        }
        catch (ArgumentException ex)
        {

            return Results.BadRequest(new { mensagem = ex.Message });
        }
    }).WithTags("User");

    app.MapPost("/Cadastro/Administrador", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
    {

        var validacao = new ErrosDeValidacao
        {
            Mensagens = new List<string>()
        };

        if (string.IsNullOrEmpty(administradorDTO.Email))
            validacao.Mensagens.Add("Nao e possivel cadastrar administrador sem Email");

        if (string.IsNullOrEmpty(administradorDTO.Senha))
            validacao.Mensagens.Add("Nao e possivel cadastrar administrador sem Senha");

        if (administradorDTO.Perfil.ToString() == null)
            validacao.Mensagens.Add("Nao e possivel cadastrar administrador sem Perfil");

        if (administradorDTO.Perfil != Perfil.Adm && administradorDTO.Perfil.ToString() != '0'.ToString())
            validacao.Mensagens.Add("Perfil deve ser Adm ou 0");

        if (validacao.Mensagens.Count > 0)
            return Results.BadRequest(validacao);

        var adm = new Administrador
        {
            Email = administradorDTO.Email,
            Senha = administradorDTO.Senha,
            Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Adm.ToString()

        };
        administradorServico.Incluir(adm);


        return Results.Created($"/Administrador/{adm.Email}", adm);

    }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"}).WithTags("User");

    app.MapGet("/Users/Listar", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
    {

        var usuarios = administradorServico.Todos(pagina)
           .Where(u => u.Perfil != Perfil.Adm.ToString())
           .ToList();

        return Results.Ok(usuarios);
    }).RequireAuthorization().WithTags("User");

    app.MapGet("/Administrador/users", (IAdministradorServico administradorServico) =>
    {
        var usuarios = administradorServico.Todos(null)
           .Where(u => u.Perfil == Perfil.Adm.ToString() || u.Perfil == '0'.ToString())
           .ToList();

        return Results.Ok(usuarios);

    }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor"}).WithTags("User");

    app.MapGet("/Cadastro/Buscar{id:int}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
    {
        var cadastro = administradorServico.BuscaPorId(id);
        if (cadastro == null)
        {
            return Results.NotFound();
        }

        //filtra o perfil usuario comum
        if (cadastro.Perfil == Perfil.Adm.ToString())
            return Results.Forbid();


        return Results.Ok(cadastro);
    }).RequireAuthorization().WithTags("User");

    app.MapGet("/Cadastro/BuscarAdm{id:int}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
    {
        var cadastro = administradorServico.BuscaPorId(id);
        if (cadastro == null)
        {
            return Results.NotFound();
        }

        //bloqueia se nao for adm 
        if (cadastro.Perfil != Perfil.Adm.ToString())
            return Results.Forbid();

        return Results.Ok(cadastro);
    }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor"}).WithTags("User");

    #endregion
#region Veiculo
    ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
    {
        var validacao = new ErrosDeValidacao
        {
            Mensagens = new List<string>()
        };

        if (string.IsNullOrEmpty(veiculoDTO.Nome))
            validacao.Mensagens.Add("Nao e possivel cadastrar veiculo sem nome");

        if (string.IsNullOrEmpty(veiculoDTO.Marca))
            validacao.Mensagens.Add("Nao e possivel cadastrar veiculo sem Marca");

        if (string.IsNullOrEmpty(veiculoDTO.Placa))
            validacao.Mensagens.Add("Nao e possivel cadastrar veiculo sem Placa");

        if (veiculoDTO.Ano < 1900 || veiculoDTO.Ano > DateTime.Now.Year)
            validacao.Mensagens.Add("Veiculo antigo demais, tente com um veiculo a cima de 1900");

        return validacao;

    }

    app.MapPost("/Veiculos/Cadastro", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
    {
        var validacao = validaDTO(veiculoDTO);
        if (validacao.Mensagens.Count > 0)
            return Results.BadRequest(validacao);


        var veiculo = new Veiculo
        {
            Nome = veiculoDTO.Nome,
            Marca = veiculoDTO.Marca,
            Ano = veiculoDTO.Ano,
            Placa = veiculoDTO.Placa
        };
        veiculoServico.Incluir(veiculo);


        return Results.Created($"/veiculos/{veiculo.Id}", veiculo);
    }).RequireAuthorization().AllowAnonymous().WithTags("Veiculos");

    //lista todos os veiculos
    app.MapGet("Veiculos/Listar", (IVeiculoServico VeiculoServico) =>
        {
            var veiculos = VeiculoServico.Todos();
            return Results.Ok(veiculos);
        }).RequireAuthorization().AllowAnonymous().WithTags("Veiculos");


    //lista os veiculos por id ordem de cadastro linkado ao id no banco
    app.MapGet("/Veiculos/Buscar{id:int}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
    {
        var veiculo = veiculoServico.BuscaPorId(id);

        if (veiculo == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(veiculo);
    }).RequireAuthorization().WithTags("Veiculos");

    //metodo para atualizar veiculos
    app.MapPut("/Veiculos/Atualiza{id:int}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
    {
        var veiculoExistente = veiculoServico.BuscaPorId(id);
        if (veiculoExistente == null)
        {
            return Results.NotFound();
        }

        var validacao = validaDTO(veiculoDTO);
        if (validacao.Mensagens.Count > 0)
            return Results.BadRequest(validacao);

        veiculoExistente.Nome = veiculoDTO.Nome;
        veiculoExistente.Marca = veiculoDTO.Marca;
        veiculoExistente.Ano = veiculoDTO.Ano;
        veiculoExistente.Placa = veiculoDTO.Placa;

        veiculoServico.Atualizar(veiculoExistente);

        return Results.Ok(veiculoExistente);
    }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor"}).WithTags("Veiculos");

    app.MapDelete("/Veiculos/Deletar{id:int}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
    {
        var veiculoExistente = veiculoServico.BuscaPorId(id);
        if (veiculoExistente == null)
        {
            return Results.NotFound();
        }

        veiculoServico.Apagar(veiculoExistente);

        return Results.NoContent();

    }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"}).WithTags("Veiculos");

    #endregion
#region App
    // Habilita Swagger

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSwagger();
    app.UseSwaggerUI();
// Roda a aplicação
app.Run();
#endregion


public record VeiculoDTO
{
    public string Nome { get; set; } = default!;
    public string Marca { get; set; } = default!;
    public int Ano { get; set; } = default!;
    public string Placa { get; set; } = default!;
}

public class LoginDTO
{
  public string Email { get; set; } = default!;
  public string Senha { get; set; } = default!;
}