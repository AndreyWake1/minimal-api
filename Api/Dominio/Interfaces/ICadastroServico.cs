
using MinimalApi.Dominio.Entidades;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Interfaces;

public interface ICadastroServico
{
    Cadastro? Login(LoginDTO loginDTO);
    Cadastro Incluir(CadastroDTO dTO);
    Cadastro? BuscaPorId(int id);
    List<Cadastro> Todos(int? pagina);
}