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

public class Casa
{
	public string Nome { get; set; } = string.Empty;
	public int Portas { get; set; }
	public int Janelas { get; set; }
}

public class CasaDto
{
	[MirrorNonReflect]
	public string Nome { get; set; } = string.Empty;
	public int Portas { get; set; }
	public int Janelas { get; set; }
}

public class CasaSemAtributoDto
{
	public string Nome { get; set; } = string.Empty;
	public int Portas { get; set; }
	public int Janelas { get; set; }
}
