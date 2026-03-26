using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class IgnoreNullTests
{
	[Fact]
	public void Quando_IgnoreNull_True_Deve_Ignorar_Valores_Nulos()
	{
		// Arrange
		var config = new MirrorConfiguration { IgnoreNullValues = true };
		var mirror = new Mirror(config);

		var origem = new { Nome = (string?)null, Idade = 25 };
		var destino = new Pessoa { Id = 1, Nome = "Original", Email = "teste@email.com" };

		// Act
		mirror.Reflect(origem, destino);

		// Assert
		Assert.Equal(1, destino.Id); // Não alterado (não existe na origem)
		Assert.Equal("Original", destino.Nome); // Não alterado (porque origem.Nome é null e IgnoreNull=true)
		Assert.Equal("teste@email.com", destino.Email); // Não alterado (não existe na origem)
	}

	[Fact]
	public void Quando_IgnoreNull_False_Deve_Sobrescrever_Com_Nulos()
	{
		// Arrange
		var config = new MirrorConfiguration { IgnoreNullValues = false };
		var mirror = new Mirror(config);

		var origem = new { Nome = (string?)null, Idade = 25 };
		var destino = new Pessoa { Id = 1, Nome = "Original", Email = "teste@email.com" };

		// Act
		mirror.Reflect(origem, destino);

		// Assert
		Assert.Equal(1, destino.Id); // Não alterado (não existe na origem)
		Assert.Null(destino.Nome); // Sobrescrito com null (IgnoreNull=false)
		Assert.Equal("teste@email.com", destino.Email); // Não alterado (não existe na origem)
	}

	[Fact]
	public void IgnoreNull_Deve_Funcionar_Com_Objetos_Complexos()
	{
		// Arrange
		var config = new MirrorConfiguration { IgnoreNullValues = true };
		var mirror = new Mirror(config);

		var origem = new ClienteDto
		{
			Id = 1,
			Nome = "João",
			Endereco = null, // Null
			Telefones = new List<string>()
		};

		var destino = new Cliente
		{
			Id = 0,
			Nome = "Original",
			Endereco = new Endereco { Logradouro = "Rua Original" },
			Telefones = new List<string> { "1199999999" }
		};

		// Act
		origem.ReflectTo(destino, mirror);

		// Assert
		Assert.Equal(1, destino.Id); // Atualizado
		Assert.Equal("João", destino.Nome); // Atualizado
		Assert.NotNull(destino.Endereco); // Não foi sobrescrito com null (IgnoreNull=true)
		Assert.Equal("Rua Original", destino.Endereco.Logradouro); // Preservado
		Assert.Empty(destino.Telefones); // Atualizado (lista vazia substitui lista com itens)
	}

	[Fact]
	public void IgnoreNull_Com_Propriedades_Nao_Existentes_Nao_Altera()
	{
		// Arrange
		var config = new MirrorConfiguration { IgnoreNullValues = true };
		var mirror = new Mirror(config);

		var origem = new { Idade = 30, Cidade = "São Paulo" }; // Não tem Id, Nome, Email
		var destino = new Pessoa { Id = 1, Nome = "Original", Email = "teste@email.com" };

		// Act
		mirror.Reflect(origem, destino);

		// Assert
		Assert.Equal(1, destino.Id); // Não alterado
		Assert.Equal("Original", destino.Nome); // Não alterado
		Assert.Equal("teste@email.com", destino.Email); // Não alterado
	}

	[Fact]
	public void IgnoreNull_Com_Listas_Deve_Substituir_Completa()
	{
		// Arrange
		var config = new MirrorConfiguration { IgnoreNullValues = true };
		var mirror = new Mirror(config);

		var origem = new ClienteDto
		{
			Telefones = new List<string>() // Lista vazia
		};

		var destino = new Cliente
		{
			Telefones = new List<string> { "1199999999", "1188888888" }
		};

		// Act
		origem.ReflectTo(destino, mirror);

		// Assert
		Assert.Empty(destino.Telefones); // Lista vazia substitui lista com itens
	}

	[Fact]
	public void IgnoreNull_Com_Valores_Nao_Nulos_Deve_Atualizar()
	{
		// Arrange
		var config = new MirrorConfiguration { IgnoreNullValues = true };
		var mirror = new Mirror(config);

		var origem = new { Nome = "João Silva", Idade = 30 };
		var destino = new Pessoa { Id = 1, Nome = "Original", Email = "teste@email.com" };

		// Act
		mirror.Reflect(origem, destino);

		// Assert
		Assert.Equal(1, destino.Id); // Não alterado
		Assert.Equal("João Silva", destino.Nome); // Atualizado (não é null)
		Assert.Equal("teste@email.com", destino.Email); // Não alterado
	}
}