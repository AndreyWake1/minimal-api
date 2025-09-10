using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MinimalApi.Dominio.Enuns;

namespace MinimalApi.Dominio.Entidades;

public class Cadastro
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get;set; } = default!;

    [Required]
    [StringLength(255)]
    public string Email { get;set; } = default!;

    [Required]
    [StringLength(50)]
    public string Senha { get;set; } = default!;

    public string? Perfil { get; set; } 
}