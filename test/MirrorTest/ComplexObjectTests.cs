using Mirror.Test.Models;

namespace Mirror.Test;

public class ComplexObjectTests
{
	[Fact]
	/// Teste para objeto com propriedade complexa simples
	public void Deve_Mapear_Objeto_Com_Propriedade_Complexa_Simples()
	{
		// Arrange
		var mirror = new Mirror();

		var cliente = new Cliente
		{
			Id = 1,
			Nome = "João Silva",
			EnderecoPrincipal = new Endereco
			{
				Logradouro = "Rua A",
				Numero = "123",
				Cep = "12345-678",
				Cidade = "São Paulo",
				Complemento = "Apto 45"
			}
		};

		// Act
		var dto = mirror.Reflect<Cliente, ClienteDto>(cliente);

		// Assert
		Assert.NotNull(dto);
		Assert.Equal(cliente.Id, dto.Id);
		Assert.Equal(cliente.Nome, dto.Nome);

		Assert.NotNull(dto.EnderecoPrincipal);
		Assert.Equal(cliente.EnderecoPrincipal.Logradouro, dto.EnderecoPrincipal.Logradouro);
		Assert.Equal(cliente.EnderecoPrincipal.Numero, dto.EnderecoPrincipal.Numero);
		Assert.Equal(cliente.EnderecoPrincipal.Cep, dto.EnderecoPrincipal.Cep);
		Assert.Equal(cliente.EnderecoPrincipal.Cidade, dto.EnderecoPrincipal.Cidade);
		Assert.Equal(cliente.EnderecoPrincipal.Complemento, dto.EnderecoPrincipal.Complemento);
	}

	[Fact]
	/// Teste para objeto com lista de objetos complexos
	public void Deve_Mapear_Objeto_Com_Lista_De_Objetos_Complexos()
	{
		// Arrange
		var mirror = new Mirror();

		var cliente = new Cliente
		{
			Id = 1,
			Nome = "João Silva",
			Enderecos = new List<Endereco>
		{
			new()
			{
				Logradouro = "Rua A",
				Numero = "123",
				Cidade = "São Paulo"
			},
			new()
			{
				Logradouro = "Rua B",
				Numero = "456",
				Cidade = "Rio de Janeiro"
			}
		}
		};

		// Act
		var dto = mirror.Reflect<Cliente, ClienteDto>(cliente);

		// Assert
		Assert.NotNull(dto);
		Assert.Equal(2, dto.Enderecos.Count);

		Assert.Equal(cliente.Enderecos[0].Logradouro, dto.Enderecos[0].Logradouro);
		Assert.Equal(cliente.Enderecos[0].Numero, dto.Enderecos[0].Numero);
		Assert.Equal(cliente.Enderecos[0].Cidade, dto.Enderecos[0].Cidade);

		Assert.Equal(cliente.Enderecos[1].Logradouro, dto.Enderecos[1].Logradouro);
		Assert.Equal(cliente.Enderecos[1].Numero, dto.Enderecos[1].Numero);
		Assert.Equal(cliente.Enderecos[1].Cidade, dto.Enderecos[1].Cidade);
	}

	[Fact]
	/// Teste para objeto com lista de tipos primitivos
	public void Deve_Mapear_Objeto_Com_Lista_De_Tipos_Primitivos()
	{
		// Arrange
		var mirror = new Mirror();

		var cliente = new Cliente
		{
			Id = 1,
			Nome = "João Silva",
			Telefones = new List<string>
		{
			"11999999999",
			"11888888888"
		}
		};

		// Act
		var dto = mirror.Reflect<Cliente, ClienteDto>(cliente);

		// Assert
		Assert.NotNull(dto);
		Assert.Equal(2, dto.Telefones.Count);
		Assert.Equal(cliente.Telefones[0], dto.Telefones[0]);
		Assert.Equal(cliente.Telefones[1], dto.Telefones[1]);
	}

	[Fact]
	/// Teste específico para seu cenário do KycEnterpriseRequest
	public void Deve_Mapear_KycEnterpriseRequest_Corretamente()
	{
		// Arrange
		var mirror = new Mirror();

		var dto = new KycEnterpriseRequestDto
		{
			ProvisionamentoId = Guid.Parse("83a0c027-3d2e-48e2-bf4d-7fefd2ff44dd"),
			Empresa = new CreateEmpresaRequestDto
			{
				RazaoSocial = "TNH LOGISTICA LTDA",
				Fantasia = "TNH LOGISTICA LTDA",
				Cnpj = "24148379000140"
			},
			Enderecos = new List<EnderecoDto>
		{
			new()
			{
				Logradouro = "Avenida Castelo Branco",
				Numero = "18-46",
				Cep = "17052005",
				Cidade = "Bauru",
				Complemento = "Sala 1"
			}
		},
			Telefones = new List<string>
		{
			"(14) 99904-0691"
		}
		};

		// Act
		var result = mirror.Reflect<KycEnterpriseRequestDto, KycEnterpriseRequest>(dto);

		// Assert
		Assert.NotNull(result);

		// Teste 1: Propriedade simples
		Assert.Equal(dto.ProvisionamentoId, result.ProvisionamentoId);

		// Teste 2: Objeto complexo aninhado
		Assert.NotNull(result.Empresa);
		Assert.Equal(dto.Empresa.RazaoSocial, result.Empresa.RazaoSocial);
		Assert.Equal(dto.Empresa.Fantasia, result.Empresa.Fantasia);
		Assert.Equal(dto.Empresa.Cnpj, result.Empresa.Cnpj);

		// Teste 3: Lista de objetos complexos
		Assert.NotNull(result.Enderecos);
		Assert.Single(result.Enderecos);
		Assert.Equal(dto.Enderecos[0].Logradouro, result.Enderecos[0].Logradouro);
		Assert.Equal(dto.Enderecos[0].Numero, result.Enderecos[0].Numero);
		Assert.Equal(dto.Enderecos[0].Cep, result.Enderecos[0].Cep);
		Assert.Equal(dto.Enderecos[0].Cidade, result.Enderecos[0].Cidade);
		Assert.Equal(dto.Enderecos[0].Complemento, result.Enderecos[0].Complemento);

		// Teste 4: Lista de tipos primitivos
		Assert.NotNull(result.Telefones);
		Assert.Single(result.Telefones);
		Assert.Equal(dto.Telefones[0], result.Telefones[0]);
	}

	[Fact]
	/// Teste para verificar o mapeamento profundo com diferentes níveis
	public void Deve_Mapear_Objeto_Com_Multiplos_Niveis_De_Aninhamento()
	{
		// Arrange
		var mirror = new Mirror();

		var dto = new KycEnterpriseRequestDto
		{
			ProvisionamentoId = Guid.NewGuid(),
			Empresa = new CreateEmpresaRequestDto
			{
				RazaoSocial = "Empresa Teste",
				Fantasia = "Teste",
				Cnpj = "12345678000195"
			},
			Enderecos = new List<EnderecoDto>
		{
			new()
			{
				Logradouro = "Rua Principal",
				Numero = "100",
				Cidade = "São Paulo"
			}
		}
		};

		// Act
		var result = mirror.Reflect<KycEnterpriseRequestDto, KycEnterpriseRequest>(dto);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Empresa);
		Assert.NotNull(result.Enderecos);
		Assert.Single(result.Enderecos);

		// Verifica se todos os níveis foram mapeados
		Assert.Equal(dto.Empresa.RazaoSocial, result.Empresa.RazaoSocial);
		Assert.Equal(dto.Enderecos[0].Logradouro, result.Enderecos[0].Logradouro);
	}
}
