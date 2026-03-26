namespace Mirror.Test.Models;

// Modelos Simples
public class Pessoa
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public DateTime DataNascimento { get; set; }
}

public class PessoaDto
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public int Idade { get; set; }
}