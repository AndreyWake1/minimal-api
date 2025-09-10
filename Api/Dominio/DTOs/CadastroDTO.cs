
using System.ComponentModel.DataAnnotations;
using MinimalApi.Dominio.Enuns;

namespace MinimalApi.DTOs;
public class CadastroDTO
{
    [Required(ErrorMessage = "O campo Email é obrigatório")]
    public string Email { get; set; } = default!;
    
    [Required(ErrorMessage = "O campo Senha é obrigatório")]
    [StringLength(50, ErrorMessage = "A senha deve ter no máximo 50 caracteres")]
    public string Senha { get; set; } = default!;
    public object Perfil { get;set; } = default!;
}