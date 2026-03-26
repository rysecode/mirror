using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class BasicMappingTests
{
	[Fact]
	public void Deve_Mapear_Propriedades_Com_Nomes_Iguais()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoa = new Pessoa
		{
			Id = 1,
			Nome = "João Silva",
			Email = "joao@email.com",
			DataNascimento = new DateTime(1990, 1, 1)
		};

		// Act
		var dto = mirror.Reflect<Pessoa, PessoaDto>(pessoa);

		// Assert
		Assert.NotNull(dto);
		Assert.Equal(pessoa.Id, dto.Id);
		Assert.Equal(pessoa.Nome, dto.Nome);
		Assert.Equal(pessoa.Email, dto.Email);
		// Idade não é mapeada porque não existe na origem
	}

	[Fact]
	public void Deve_Mapear_Com_Objeto_Existente()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoa = new Pessoa { Id = 1, Nome = "João" };
		var dto = new PessoaDto { Id = 0, Nome = "Original" };

		// Act
		mirror.Reflect(pessoa, dto);

		// Assert
		Assert.Equal(1, dto.Id);
		Assert.Equal("João", dto.Nome);
	}

	[Fact]
	public void Deve_Mapear_Colecoes()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoas = new List<Pessoa>
		{
			new() { Id = 1, Nome = "João" },
			new() { Id = 2, Nome = "Maria" }
		};

		// Act
		var dtos = pessoas.ReflectAll<Pessoa, PessoaDto>(mirror).ToList();

		// Assert
		Assert.Equal(2, dtos.Count);
		Assert.Equal(1, dtos[0].Id);
		Assert.Equal("João", dtos[0].Nome);
		Assert.Equal(2, dtos[1].Id);
		Assert.Equal("Maria", dtos[1].Nome);
	}
}
