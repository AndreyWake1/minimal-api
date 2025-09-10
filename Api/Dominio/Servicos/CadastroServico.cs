using MinimalApi.Dominio.Entidades;
using MinimalApi.Infraestrutura.Db;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using System.Text.Json;
using MinimalApi.Dominio.Enuns;

namespace MinimalApi.Dominio.Servicos;

public class CadastroServico : ICadastroServico
{
    private readonly DbContexto _contexto;
    public CadastroServico(DbContexto contexto)
    {
        _contexto = contexto;
    }

    public Cadastro? Login(LoginDTO loginDTO)
    {
        return _contexto.Cadastro.Where(c => c.Email == loginDTO.Email && c.Senha == loginDTO.Senha).FirstOrDefault();
    }
    public Cadastro? BuscaPorId(int id)
    {
        return _contexto.Cadastro.Where(c => c.Id == id).FirstOrDefault();
    }

public Cadastro Incluir(CadastroDTO dTO)
{
    string perfilParaSalvar;

    // Converte o 'object' para uma string 
    var perfilString = dTO.Perfil.ToString();

    // Tenta interpretar a string como um número de Enum válido
    if (int.TryParse(perfilString, out int perfilInt) && Enum.IsDefined(typeof(Perfil), perfilInt))
    {
        //converte o int para o nome 
        perfilParaSalvar = ((Perfil)perfilInt).ToString();
    }
    else if (Enum.TryParse<Perfil>(perfilString, ignoreCase: true, out var perfilEnum))
    {
        //usa o texto validado
        perfilParaSalvar = perfilEnum.ToString();
    }
    else
    {
        
        throw new ArgumentException("Valor de Perfil inválido.", nameof(dTO.Perfil));
    }

     //trabalha com a string validada
        var cadastro = new Cadastro
    {
        Email = dTO.Email,
        Senha = dTO.Senha, 
        Perfil = perfilParaSalvar
    };

    _contexto.Cadastro.Add(cadastro);
    _contexto.SaveChanges();

    return cadastro;
}

    public List<Cadastro> Todos(int? pagina = 1)
    {
        var query = _contexto.Cadastro.AsQueryable();

        int itensPorPagina = 10;

        if (pagina != null)
            query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);

        return query.ToList();
    }
}
