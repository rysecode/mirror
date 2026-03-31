using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class DeepHybridGraphTests
{
	[Fact]
	public void Deve_Mapear_Lista_Profunda_Com_Dictionaries_E_Nulls()
	{
		var mirror = new Mirror();
		var origem = new List<ClienteHibrido>
		{
			new()
			{
				Id = 1,
				Observacao = null,
				Pessoa = new PessoaDetalhadaComMapas
				{
					Nome = "Ana",
					EnderecoPrincipal = new Endereco
					{
						Logradouro = "Rua Central",
						Numero = "100",
						Cidade = "São Paulo",
						Cep = "01000-000"
					},
					Tags = new List<string> { "vip", "ativo" },
					Enderecos = new List<Endereco?>
					{
						new() { Logradouro = "Rua A", Numero = "10", Cidade = "Campinas" },
						null
					},
					Contatos = new List<ContatoDetalhado?>
					{
						new() { Tipo = "email", Valor = "ana@teste.com" },
						null
					},
					EnderecosPorTipo = new Dictionary<string, Endereco?>
					{
						["residencial"] = new Endereco { Logradouro = "Rua Casa", Numero = "20", Cidade = "Santos" },
						["comercial"] = null
					},
					ContatosPorCategoria = new Dictionary<string, List<ContatoDetalhado?>>
					{
						["principal"] =
						[
							new ContatoDetalhado { Tipo = "telefone", Valor = "11999999999" },
							null
						],
						["secundario"] =
						[
							new ContatoDetalhado { Tipo = "email", Valor = "ana.sec@teste.com" }
						]
					},
					Preferencias = new SortedList<string, PreferenciaCanal?>
					{
						["email"] = new PreferenciaCanal { Canal = "email", Ativo = true },
						["sms"] = null
					}
				}
			}
		};

		var destino = origem.ReflectAll<ClienteHibrido, ClienteHibridoDto>(mirror).ToList();

		Assert.Single(destino);
		Assert.Equal(1, destino[0].Id);
		Assert.Null(destino[0].Observacao);
		var pessoa = Assert.IsType<PessoaDetalhadaComMapasDto>(destino[0].Pessoa);
		Assert.Equal("Ana", pessoa.Nome);
		var enderecoPrincipal = Assert.IsType<EnderecoDto>(pessoa.EnderecoPrincipal);
		Assert.Equal("Rua Central", enderecoPrincipal.Logradouro);
		Assert.Equal(new[] { "vip", "ativo" }, pessoa.Tags);

		Assert.Equal(2, pessoa.Enderecos.Count);
		var primeiroEndereco = Assert.IsType<EnderecoDto>(pessoa.Enderecos[0]);
		Assert.Equal("Rua A", primeiroEndereco.Logradouro);
		Assert.Null(pessoa.Enderecos[1]);

		Assert.Equal(2, pessoa.Contatos.Count);
		var primeiroContato = Assert.IsType<ContatoDetalhadoDto>(pessoa.Contatos[0]);
		Assert.Equal("email", primeiroContato.Tipo);
		Assert.Null(pessoa.Contatos[1]);

		Assert.Equal(2, pessoa.EnderecosPorTipo.Count);
		var enderecoResidencial = Assert.IsType<EnderecoDto>(pessoa.EnderecosPorTipo["residencial"]);
		Assert.Equal("Santos", enderecoResidencial.Cidade);
		Assert.Null(pessoa.EnderecosPorTipo["comercial"]);

		Assert.Equal(2, pessoa.ContatosPorCategoria.Count);
		Assert.Equal(2, pessoa.ContatosPorCategoria["principal"].Count);
		var contatoPrincipal = Assert.IsType<ContatoDetalhadoDto>(pessoa.ContatosPorCategoria["principal"][0]);
		Assert.Equal("telefone", contatoPrincipal.Tipo);
		Assert.Null(pessoa.ContatosPorCategoria["principal"][1]);
		Assert.Single(pessoa.ContatosPorCategoria["secundario"]);
		var contatoSecundario = Assert.IsType<ContatoDetalhadoDto>(pessoa.ContatosPorCategoria["secundario"][0]);
		Assert.Equal("ana.sec@teste.com", contatoSecundario.Valor);

		Assert.Equal(2, pessoa.Preferencias.Count);
		var preferenciaEmail = Assert.IsType<PreferenciaCanalDto>(pessoa.Preferencias["email"]);
		Assert.Equal("email", preferenciaEmail.Canal);
		Assert.True(preferenciaEmail.Ativo);
		Assert.Null(pessoa.Preferencias["sms"]);
	}

	[Fact]
	public void IgnoreNull_Deve_Preservar_Valores_Existentes_Em_Grafo_Hibrido()
	{
		var mirror = new Mirror(new MirrorConfiguration { IgnoreNullValues = true });
		var origem = new ClienteHibrido
		{
			Id = 7,
			Observacao = null,
			Pessoa = new PessoaDetalhadaComMapas
			{
				Nome = "Cliente Atualizado",
				EnderecoPrincipal = null,
				Tags = new List<string> { "novo" },
				Enderecos = new List<Endereco?>(),
				Contatos = new List<ContatoDetalhado?>(),
				EnderecosPorTipo = new Dictionary<string, Endereco?>
				{
					["entrega"] = null
				},
				ContatosPorCategoria = new Dictionary<string, List<ContatoDetalhado?>>
				{
					["principal"] = []
				},
				Preferencias = new SortedList<string, PreferenciaCanal?>
				{
					["push"] = null
				}
			}
		};

		var destino = new ClienteHibridoDto
		{
			Id = 0,
			Observacao = "manter",
			Pessoa = new PessoaDetalhadaComMapasDto
			{
				Nome = "Original",
				EnderecoPrincipal = new EnderecoDto { Logradouro = "Endereco Antigo" },
				Tags = new List<string> { "antigo" },
				Enderecos = new List<EnderecoDto?> { new EnderecoDto { Logradouro = "Rua Antiga" } },
				Contatos = new List<ContatoDetalhadoDto?> { new ContatoDetalhadoDto { Tipo = "email", Valor = "old@teste.com" } },
				EnderecosPorTipo = new Dictionary<string, EnderecoDto?>
				{
					["entrega"] = new EnderecoDto { Logradouro = "Deposito" }
				},
				ContatosPorCategoria = new Dictionary<string, List<ContatoDetalhadoDto?>>
				{
					["principal"] =
					[
						new ContatoDetalhadoDto { Tipo = "telefone", Valor = "1133333333" }
					]
				},
				Preferencias = new SortedList<string, PreferenciaCanalDto?>
				{
					["push"] = new PreferenciaCanalDto { Canal = "push", Ativo = true }
				}
			}
		};

		mirror.Reflect(origem, destino);

		Assert.Equal(7, destino.Id);
		Assert.Equal("manter", destino.Observacao);
		var pessoa = Assert.IsType<PessoaDetalhadaComMapasDto>(destino.Pessoa);
		Assert.Equal("Cliente Atualizado", pessoa.Nome);
		var enderecoPrincipal = Assert.IsType<EnderecoDto>(pessoa.EnderecoPrincipal);
		Assert.Equal("Endereco Antigo", enderecoPrincipal.Logradouro);
		Assert.Equal(new[] { "novo" }, pessoa.Tags);
		Assert.Empty(pessoa.Enderecos);
		Assert.Empty(pessoa.Contatos);
		Assert.Single(pessoa.EnderecosPorTipo);
		Assert.Null(pessoa.EnderecosPorTipo["entrega"]);
		Assert.Single(pessoa.ContatosPorCategoria);
		Assert.Empty(pessoa.ContatosPorCategoria["principal"]);
		Assert.Single(pessoa.Preferencias);
		Assert.Null(pessoa.Preferencias["push"]);
	}
}
