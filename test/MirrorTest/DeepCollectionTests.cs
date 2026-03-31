using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class DeepCollectionTests
{
	[Fact]
	public void Deve_Mapear_Lista_Raiz_Com_Itens_Complexos_E_Listas_Internas()
	{
		var mirror = new Mirror();
		var origem = new List<ClienteProfundo>
		{
			new()
			{
				Id = 1,
				Pessoa = new PessoaDetalhada
				{
					Nome = "Ana",
					Tags = new List<string> { "vip", "newsletter" },
					Enderecos = new List<Endereco>
					{
						new() { Logradouro = "Rua A", Numero = "10", Cidade = "São Paulo", Cep = "01000-000" },
						new() { Logradouro = "Rua B", Numero = "20", Cidade = "Campinas", Cep = "02000-000" }
					},
					Contatos = new List<ContatoDetalhado>
					{
						new() { Tipo = "email", Valor = "ana@teste.com" },
						new() { Tipo = "telefone", Valor = "11999999999" }
					}
				}
			},
			new()
			{
				Id = 2,
				Pessoa = new PessoaDetalhada
				{
					Nome = "Bruno",
					Tags = new List<string> { "lead" },
					Enderecos = new List<Endereco>
					{
						new() { Logradouro = "Rua C", Numero = "30", Cidade = "Santos", Cep = "03000-000" }
					},
					Contatos = new List<ContatoDetalhado>
					{
						new() { Tipo = "email", Valor = "bruno@teste.com" }
					}
				}
			}
		};

		var destino = origem.ReflectAll<ClienteProfundo, ClienteProfundoDto>(mirror).ToList();

		Assert.Equal(2, destino.Count);

		Assert.Equal(1, destino[0].Id);
		Assert.Equal("Ana", destino[0].Pessoa.Nome);
		Assert.Equal(2, destino[0].Pessoa.Tags.Count);
		Assert.Equal("vip", destino[0].Pessoa.Tags[0]);
		Assert.Equal("newsletter", destino[0].Pessoa.Tags[1]);
		Assert.Equal(2, destino[0].Pessoa.Enderecos.Count);
		Assert.Equal("Rua A", destino[0].Pessoa.Enderecos[0].Logradouro);
		Assert.Equal("Campinas", destino[0].Pessoa.Enderecos[1].Cidade);
		Assert.Equal(2, destino[0].Pessoa.Contatos.Count);
		Assert.Equal("email", destino[0].Pessoa.Contatos[0].Tipo);
		Assert.Equal("11999999999", destino[0].Pessoa.Contatos[1].Valor);

		Assert.Equal(2, destino[1].Id);
		Assert.Equal("Bruno", destino[1].Pessoa.Nome);
		Assert.Single(destino[1].Pessoa.Tags);
		Assert.Equal("lead", destino[1].Pessoa.Tags[0]);
		Assert.Single(destino[1].Pessoa.Enderecos);
		Assert.Equal("Rua C", destino[1].Pessoa.Enderecos[0].Logradouro);
		Assert.Single(destino[1].Pessoa.Contatos);
		Assert.Equal("bruno@teste.com", destino[1].Pessoa.Contatos[0].Valor);
	}

	[Fact]
	public void Deve_Mapear_Lista_Raiz_Profunda_Com_Objeto_Destino_Existente()
	{
		var mirror = new Mirror();
		var origem = new ClienteProfundo
		{
			Id = 10,
			Pessoa = new PessoaDetalhada
			{
				Nome = "Cliente Atualizado",
				Tags = new List<string> { "ativo", "premium" },
				Enderecos = new List<Endereco>
				{
					new() { Logradouro = "Rua Nova", Numero = "100", Cidade = "Curitiba" }
				},
				Contatos = new List<ContatoDetalhado>
				{
					new() { Tipo = "telefone", Valor = "41999999999" }
				}
			}
		};

		var destino = new ClienteProfundoDto
		{
			Id = 0,
			Pessoa = new PessoaDetalhadaDto
			{
				Nome = "Original",
				Tags = new List<string> { "antigo" },
				Enderecos = new List<EnderecoDto>
				{
					new() { Logradouro = "Rua Antiga" }
				},
				Contatos = new List<ContatoDetalhadoDto>
				{
					new() { Tipo = "email", Valor = "original@teste.com" }
				}
			}
		};

		mirror.Reflect(origem, destino);

		Assert.Equal(10, destino.Id);
		Assert.Equal("Cliente Atualizado", destino.Pessoa.Nome);
		Assert.Equal(new[] { "ativo", "premium" }, destino.Pessoa.Tags);
		Assert.Single(destino.Pessoa.Enderecos);
		Assert.Equal("Rua Nova", destino.Pessoa.Enderecos[0].Logradouro);
		Assert.Single(destino.Pessoa.Contatos);
		Assert.Equal("41999999999", destino.Pessoa.Contatos[0].Valor);
	}
}
