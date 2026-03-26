namespace Mirror.Test.Models;

// Modelos para teste de objetos complexos
public class Endereco
{
	public string Logradouro { get; set; } = string.Empty;
	public string Numero { get; set; } = string.Empty;
	public string Cep { get; set; } = string.Empty;
	public string Cidade { get; set; } = string.Empty;
	public string? Complemento { get; set; }
}

public class EnderecoDto
{
	public string Logradouro { get; set; } = string.Empty;
	public string Numero { get; set; } = string.Empty;
	public string Cep { get; set; } = string.Empty;
	public string Cidade { get; set; } = string.Empty;
	public string? Complemento { get; set; }
}

public class Cliente
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public Endereco? Endereco { get; set; }
	public Endereco EnderecoPrincipal { get; set; } = new();
	public List<Endereco> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

public class ClienteDto
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public Endereco? Endereco { get; set; }
	public EnderecoDto EnderecoPrincipal { get; set; } = new();
	public List<EnderecoDto> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

// Modelos específicos do seu caso
// Versão do EmpresaRequest com construtor público (para testes com new())
public class EmpresaRequest
{
	public string RazaoSocial { get; set; } = string.Empty;
	public string Fantasia { get; set; } = string.Empty;
	public string Cnpj { get; set; } = string.Empty;

	// Construtor público para o teste com new()
	public EmpresaRequest() { }

	// Método Create para o factory
	public static EmpresaRequest Create(string razaoSocial, string fantasia, string cnpj)
	{
		return new EmpresaRequest
		{
			RazaoSocial = razaoSocial,
			Fantasia = fantasia,
			Cnpj = cnpj
		};
	}
}

public class EmpresaRequestFactory
{
	public string RazaoSocial { get; set; } = string.Empty;
	public string Fantasia { get; set; } = string.Empty;
	public string Cnpj { get; set; } = string.Empty;

	private EmpresaRequestFactory() { }

	public static EmpresaRequestFactory Create(string razaoSocial, string fantasia, string cnpj)
	{
		return new EmpresaRequestFactory
		{
			RazaoSocial = razaoSocial,
			Fantasia = fantasia,
			Cnpj = cnpj,
		};
	}
}

// Modelo com validações reais
public class EmpresaRequestComValidacao
{
	public string RazaoSocial { get; set; }
	public string Fantasia { get; set; }
	public string Cnpj { get; set; }

	private EmpresaRequestComValidacao(string razaoSocial, string fantasia, string cnpj)
	{
		RazaoSocial = razaoSocial;
		Fantasia = fantasia;
		Cnpj = cnpj;
	}

	public static EmpresaRequestComValidacao Create(string razaoSocial, string fantasia, string cnpj)
	{
		// Validações
		if (string.IsNullOrWhiteSpace(razaoSocial))
			throw new ArgumentException("Razão social é obrigatória");

		if (string.IsNullOrWhiteSpace(fantasia))
			throw new ArgumentException("Fantasia é obrigatória");

		if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14)
			throw new ArgumentException("CNPJ inválido");

		return new EmpresaRequestComValidacao(razaoSocial, fantasia, cnpj);
	}
}

public class CreateEmpresaRequestDto
{
	public string RazaoSocial { get; set; } = string.Empty;
	public string Fantasia { get; set; } = string.Empty;
	public string Cnpj { get; set; } = string.Empty;
}

public class KycEnterpriseRequest
{
	public Guid ProvisionamentoId { get; set; }
	public EmpresaRequest Empresa { get; set; } = new();
	public List<Endereco> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

public class KycEnterpriseRequestDto
{
	public Guid ProvisionamentoId { get; set; }
	public CreateEmpresaRequestDto Empresa { get; set; } = new();
	public List<EnderecoDto> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}
