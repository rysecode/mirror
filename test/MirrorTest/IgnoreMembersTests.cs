using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class IgnoreMembersTests
{
	[Fact]
	public void Reflect_Com_Atributo_MirrorNonReflect_Nao_Deve_Alterar_Propriedade()
	{
		var mirror = new Mirror();
		var origem = new Casa
		{
			Nome = "Lar",
			Portas = 2,
			Janelas = 5
		};
		var destino = new CasaDto
		{
			Nome = "Apto",
			Portas = 3,
			Janelas = 4
		};

		mirror.Reflect(origem, destino);

		Assert.Equal("Apto", destino.Nome);
		Assert.Equal(2, destino.Portas);
		Assert.Equal(5, destino.Janelas);
	}

	[Fact]
	public void Reflect_Com_IgnoreMembers_Deve_Manter_Propriedade_Informada()
	{
		var mirror = new Mirror();
		var origem = new Casa
		{
			Nome = "Lar",
			Portas = 2,
			Janelas = 5
		};
		var destino = new CasaSemAtributoDto
		{
			Nome = "Apto",
			Portas = 3,
			Janelas = 4
		};

		mirror.Reflect(origem, destino, r => r.Nome);

		Assert.Equal("Apto", destino.Nome);
		Assert.Equal(2, destino.Portas);
		Assert.Equal(5, destino.Janelas);
	}

	[Fact]
	public void Reflect_Com_Multiplos_IgnoreMembers_Deve_Manter_Todas_As_Propriedades_Informadas()
	{
		var mirror = new Mirror();
		var origem = new Casa
		{
			Nome = "Lar",
			Portas = 2,
			Janelas = 5
		};
		var destino = new CasaSemAtributoDto
		{
			Nome = "Apto",
			Portas = 3,
			Janelas = 4
		};

		mirror.Reflect(origem, destino, r => r.Nome, r => r.Janelas);

		Assert.Equal("Apto", destino.Nome);
		Assert.Equal(2, destino.Portas);
		Assert.Equal(4, destino.Janelas);
	}

	[Fact]
	public void Reflect_Extension_Com_IgnoreMembers_Deve_Funcionar()
	{
		var origem = new Casa
		{
			Nome = "Lar",
			Portas = 2,
			Janelas = 5
		};
		var destino = new CasaSemAtributoDto
		{
			Nome = "Apto",
			Portas = 3,
			Janelas = 4
		};

		origem.ReflectTo(destino, r => r.Nome);

		Assert.Equal("Apto", destino.Nome);
		Assert.Equal(2, destino.Portas);
		Assert.Equal(5, destino.Janelas);
	}

	[Fact]
	public void Reflect_Novo_Objeto_Com_IgnoreMembers_Deve_Preservar_Valor_Default_Do_Destino()
	{
		var mirror = new Mirror();
		var origem = new Casa
		{
			Nome = "Lar",
			Portas = 2,
			Janelas = 5
		};

		var destino = mirror.Reflect<Casa, CasaSemAtributoDto>(origem, r => r.Nome);

		Assert.Equal(string.Empty, destino.Nome);
		Assert.Equal(2, destino.Portas);
		Assert.Equal(5, destino.Janelas);
	}
}
