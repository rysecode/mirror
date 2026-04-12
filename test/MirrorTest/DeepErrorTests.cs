using Mirror.Exceptions;
using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class DeepErrorTests
{
	[Fact]
	public void ReflectSafe_Deve_Encapsular_Erro_Em_Lista_Aninhada()
	{
		var config = new MirrorConfiguration();
		config.AddFactory<ContatoDetalhado, ContatoDetalhadoDto>(contato =>
		{
			if (string.IsNullOrWhiteSpace(contato.Valor))
				throw new InvalidOperationException("Contato inválido");

			return new ContatoDetalhadoDto
			{
				Tipo = contato.Tipo,
				Valor = contato.Valor
			};
		});

		var mirror = new Mirror(config);
		var origem = new ClienteProfundo
		{
			Id = 1,
			Pessoa = new PessoaDetalhada
			{
				Nome = "Ana",
				Contatos = new List<ContatoDetalhado>
				{
					new() { Tipo = "email", Valor = "" }
				}
			}
		};

		var exception = Assert.Throws<MirrorException>(() =>
			origem.ReflectSafe<ClienteProfundoDto>(mirror));

		Assert.Contains("ClienteProfundo", exception.Message);
		Assert.Contains("ClienteProfundoDto", exception.Message);
		Assert.IsType<MirrorException>(exception.InnerException);
		Assert.Contains("Pessoa", exception.InnerException!.Message);
		Assert.IsType<MirrorException>(exception.InnerException.InnerException);
		Assert.Contains("Contatos", exception.InnerException.InnerException!.Message);
		Assert.IsType<MirrorException>(exception.InnerException.InnerException.InnerException);
		Assert.Contains("Contatos[0]", exception.InnerException.InnerException.InnerException!.Message);
		Assert.IsType<InvalidOperationException>(exception.InnerException.InnerException.InnerException.InnerException);
		Assert.Contains("Contato inválido", exception.InnerException.InnerException.InnerException.InnerException!.Message);
	}

	[Fact]
	public void ReflectSafe_Deve_Encapsular_Erro_Em_Dictionary_Aninhado()
	{
		var config = new MirrorConfiguration();
		config.AddFactory<ContatoDetalhado, ContatoDetalhadoDto>(contato =>
		{
			if (string.IsNullOrWhiteSpace(contato.Valor))
				throw new InvalidOperationException("Contato inválido em mapa");

			return new ContatoDetalhadoDto
			{
				Tipo = contato.Tipo,
				Valor = contato.Valor
			};
		});

		var mirror = new Mirror(config);
		var origem = new ClienteHibrido
		{
			Id = 2,
			Pessoa = new PessoaDetalhadaComMapas
			{
				Nome = "Bruno",
				ContatosPorCategoria = new Dictionary<string, List<ContatoDetalhado?>>
				{
					["principal"] =
					[
						new ContatoDetalhado { Tipo = "telefone", Valor = "" }
					]
				}
			}
		};

		var exception = Assert.Throws<MirrorException>(() =>
			origem.ReflectSafe<ClienteHibridoDto>(mirror));

		Assert.Contains("ClienteHibrido", exception.Message);
		Assert.Contains("ClienteHibridoDto", exception.Message);
		Assert.IsType<MirrorException>(exception.InnerException);
		Assert.Contains("Pessoa", exception.InnerException!.Message);
		Assert.IsType<MirrorException>(exception.InnerException.InnerException);
		Assert.Contains("ContatosPorCategoria", exception.InnerException.InnerException!.Message);
		Assert.IsType<MirrorException>(exception.InnerException.InnerException.InnerException);
		Assert.Contains("ContatosPorCategoria[principal][0]", exception.InnerException.InnerException.InnerException!.Message);
		Assert.IsType<InvalidOperationException>(exception.InnerException.InnerException.InnerException.InnerException);
		Assert.Contains("Contato inválido em mapa", exception.InnerException.InnerException.InnerException.InnerException!.Message);
	}
}
