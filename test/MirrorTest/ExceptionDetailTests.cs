using Mirror.Exceptions;

namespace Mirror.Test;

public class ExceptionDetailTests
{
	[Fact]
	public void Reflect_Deve_Informar_Membro_E_Tipos_Em_Erro_De_Propriedade_Simples()
	{
		var mirror = new Mirror();
		var origem = new OrigemSimplesComFalha { Idade = "abc" };

		var exception = Assert.Throws<MirrorException>(() =>
			mirror.Reflect<OrigemSimplesComFalha, DestinoSimplesComFalha>(origem));

		Assert.Equal("PropertyMapping", exception.Stage);
		Assert.Equal("Idade", exception.Path);
		Assert.Equal("Idade", exception.MemberName);
		Assert.Equal(typeof(OrigemSimplesComFalha), exception.SourceType);
		Assert.Equal(typeof(DestinoSimplesComFalha), exception.DestinationType);
		Assert.Equal(typeof(string), exception.SourceMemberType);
		Assert.Equal(typeof(int), exception.DestinationMemberType);
		Assert.Contains("OrigemSimplesComFalha.Idade (String)", exception.Message);
		Assert.Contains("DestinoSimplesComFalha.Idade (Int32)", exception.Message);
		Assert.Contains("Valor atual: \"abc\"", exception.Message);
	}

	[Fact]
	public void Reflect_Deve_Informar_Caminho_Completo_Em_Erro_De_Objeto_Aninhado()
	{
		var mirror = new Mirror();
		var origem = new OrigemClienteComFalha
		{
			Endereco = new OrigemEnderecoComFalha
			{
				Cidade = "São Paulo"
			}
		};

		var exception = Assert.Throws<MirrorException>(() =>
			mirror.Reflect<OrigemClienteComFalha, DestinoClienteComFalha>(origem));

		Assert.Equal("Endereço".Normalize(), "Endereço".Normalize()); // placeholder to keep file utf8-friendly
		Assert.Equal("Endereco", exception.Path);
		Assert.Equal("Endereco", exception.MemberName);
		Assert.IsType<MirrorException>(exception.InnerException);

		var inner = (MirrorException)exception.InnerException!;
		Assert.Equal("Endereco.Cidade", inner.Path);
		Assert.Equal("Cidade", inner.MemberName);
		Assert.Equal(typeof(string), inner.SourceMemberType);
		Assert.Equal(typeof(int), inner.DestinationMemberType);
		Assert.Contains("Endereco.Cidade", inner.Message);
		Assert.Contains("\"São Paulo\"", inner.Message);
	}

	[Fact]
	public void Reflect_Deve_Informar_Indice_Em_Erro_De_Item_De_Lista()
	{
		var configuration = new MirrorConfiguration();
		configuration.AddFactory<ItemOrigemComFalha, ItemDestinoComFalha>(item =>
		{
			if (string.IsNullOrWhiteSpace(item.Codigo))
				throw new InvalidOperationException("Código inválido");

			return new ItemDestinoComFalha { Codigo = item.Codigo };
		});

		var mirror = new Mirror(configuration);
		var origem = new OrigemListaComFalha
		{
			Itens = new List<ItemOrigemComFalha>
			{
				new() { Codigo = "" }
			}
		};

		var exception = Assert.Throws<MirrorException>(() =>
			mirror.Reflect<OrigemListaComFalha, DestinoListaComFalha>(origem));

		Assert.Equal("Itens", exception.Path);
		Assert.IsType<MirrorException>(exception.InnerException);

		var inner = (MirrorException)exception.InnerException!;
		Assert.Equal("Factory", inner.Stage);
		Assert.Equal("Itens[0]", inner.Path);
		Assert.Contains("Itens[0]", inner.Message);
		Assert.IsType<InvalidOperationException>(inner.InnerException);
		Assert.Contains("Código inválido", inner.InnerException!.Message);
	}

	[Fact]
	public void Reflect_Deve_Informar_Chave_Em_Erro_De_Valor_De_Dicionario()
	{
		var configuration = new MirrorConfiguration();
		configuration.AddFactory<ItemOrigemComFalha, ItemDestinoComFalha>(item =>
		{
			if (string.IsNullOrWhiteSpace(item.Codigo))
				throw new InvalidOperationException("Valor inválido no dicionário");

			return new ItemDestinoComFalha { Codigo = item.Codigo };
		});

		var mirror = new Mirror(configuration);
		var origem = new OrigemDicionarioComFalha
		{
			Itens = new Dictionary<string, ItemOrigemComFalha>
			{
				["principal"] = new ItemOrigemComFalha { Codigo = "" }
			}
		};

		var exception = Assert.Throws<MirrorException>(() =>
			mirror.Reflect<OrigemDicionarioComFalha, DestinoDicionarioComFalha>(origem));

		Assert.Equal("Itens", exception.Path);
		Assert.IsType<MirrorException>(exception.InnerException);

		var inner = (MirrorException)exception.InnerException!;
		Assert.Equal("Factory", inner.Stage);
		Assert.Equal("Itens[principal]", inner.Path);
		Assert.Contains("Itens[principal]", inner.Message);
		Assert.IsType<InvalidOperationException>(inner.InnerException);
		Assert.Contains("Valor inválido no dicionário", inner.InnerException!.Message);
	}

	private sealed class OrigemSimplesComFalha
	{
		public string Idade { get; set; } = string.Empty;
	}

	private sealed class DestinoSimplesComFalha
	{
		public int Idade { get; set; }
	}

	private sealed class OrigemClienteComFalha
	{
		public OrigemEnderecoComFalha Endereco { get; set; } = new();
	}

	private sealed class DestinoClienteComFalha
	{
		public DestinoEnderecoComFalha Endereco { get; set; } = new();
	}

	private sealed class OrigemEnderecoComFalha
	{
		public string Cidade { get; set; } = string.Empty;
	}

	private sealed class DestinoEnderecoComFalha
	{
		public int Cidade { get; set; }
	}

	private sealed class OrigemListaComFalha
	{
		public List<ItemOrigemComFalha> Itens { get; set; } = new();
	}

	private sealed class DestinoListaComFalha
	{
		public List<ItemDestinoComFalha> Itens { get; set; } = new();
	}

	private sealed class OrigemDicionarioComFalha
	{
		public Dictionary<string, ItemOrigemComFalha> Itens { get; set; } = new();
	}

	private sealed class DestinoDicionarioComFalha
	{
		public Dictionary<string, ItemDestinoComFalha> Itens { get; set; } = new();
	}

	private sealed class ItemOrigemComFalha
	{
		public string Codigo { get; set; } = string.Empty;
	}

	private sealed class ItemDestinoComFalha
	{
		public string Codigo { get; set; } = string.Empty;
	}
}
