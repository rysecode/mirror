using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Mirror.Test.Models;

namespace Mirror.Test;

public class DictionaryMappingTests
{
	[Fact]
	public void Deve_Mapear_Dictionary_Com_Valores_Complexos()
	{
		var mirror = new Mirror();
		var origem = new PessoaMapasOrigem
		{
			DictionaryPessoas = new Dictionary<string, Pessoa>
			{
				["ana"] = new Pessoa { Id = 1, Nome = "Ana", Email = "ana@teste.com" },
				["bruno"] = new Pessoa { Id = 2, Nome = "Bruno", Email = "bruno@teste.com" }
			}
		};

		var destino = mirror.Reflect<PessoaMapasOrigem, PessoaMapasDestino>(origem);

		Assert.IsType<Dictionary<string, PessoaDto>>(destino.DictionaryPessoas);
		Assert.Equal(2, destino.DictionaryPessoas.Count);
		Assert.Equal("Ana", destino.DictionaryPessoas["ana"].Nome);
		Assert.Equal(1, destino.DictionaryPessoas["ana"].Id);
		Assert.Equal("Bruno", destino.DictionaryPessoas["bruno"].Nome);
		Assert.Equal(2, destino.DictionaryPessoas["bruno"].Id);
	}

	[Fact]
	public void Deve_Mapear_SortedList_Com_Valores_Complexos()
	{
		var mirror = new Mirror();
		var origem = new PessoaMapasOrigem
		{
			SortedListPessoas = new SortedList<string, Pessoa>
			{
				["bruno"] = new Pessoa { Id = 2, Nome = "Bruno", Email = "bruno@teste.com" },
				["ana"] = new Pessoa { Id = 1, Nome = "Ana", Email = "ana@teste.com" }
			}
		};

		var destino = mirror.Reflect<PessoaMapasOrigem, PessoaMapasDestino>(origem);

		Assert.IsType<SortedList<string, PessoaDto>>(destino.SortedListPessoas);
		Assert.Equal(2, destino.SortedListPessoas.Count);
		Assert.Equal("ana", destino.SortedListPessoas.Keys[0]);
		Assert.Equal("Ana", destino.SortedListPessoas["ana"].Nome);
		Assert.Equal("Bruno", destino.SortedListPessoas["bruno"].Nome);
	}

	[Fact]
	public void Deve_Mapear_Dictionary_Com_Valores_Simples()
	{
		var mirror = new Mirror();
		var origem = new TelefoneMapasOrigem
		{
			DictionaryTelefones = new Dictionary<int, string>
			{
				[1] = "11999999999",
				[2] = "11888888888"
			}
		};

		var destino = mirror.Reflect<TelefoneMapasOrigem, TelefoneMapasDestino>(origem);

		Assert.IsType<Dictionary<int, string>>(destino.DictionaryTelefones);
		Assert.Equal(2, destino.DictionaryTelefones.Count);
		Assert.Equal("11999999999", destino.DictionaryTelefones[1]);
		Assert.Equal("11888888888", destino.DictionaryTelefones[2]);
	}

	[Fact]
	public void Deve_Mapear_SortedList_Com_Valores_Simples()
	{
		var mirror = new Mirror();
		var origem = new TelefoneMapasOrigem
		{
			SortedListTelefones = new SortedList<int, string>
			{
				[2] = "11888888888",
				[1] = "11999999999"
			}
		};

		var destino = mirror.Reflect<TelefoneMapasOrigem, TelefoneMapasDestino>(origem);

		Assert.IsType<SortedList<int, string>>(destino.SortedListTelefones);
		Assert.Equal(2, destino.SortedListTelefones.Count);
		Assert.Equal(1, destino.SortedListTelefones.Keys[0]);
		Assert.Equal("11999999999", destino.SortedListTelefones[1]);
		Assert.Equal("11888888888", destino.SortedListTelefones[2]);
	}

	[Fact]
	public void Deve_Mapear_IReadOnlyDictionary_E_ReadOnlyDictionary()
	{
		var mirror = new Mirror();
		var origem = new PessoaMapasOrigem
		{
			IReadOnlyDictionaryPessoas = new Dictionary<string, Pessoa>
			{
				["ana"] = new Pessoa { Id = 1, Nome = "Ana" }
			},
			ReadOnlyDictionaryPessoas = new ReadOnlyDictionary<string, Pessoa>(
				new Dictionary<string, Pessoa>
				{
					["bruno"] = new Pessoa { Id = 2, Nome = "Bruno" }
				})
		};

		var destino = mirror.Reflect<PessoaMapasOrigem, PessoaMapasDestino>(origem);

		Assert.IsType<Dictionary<string, PessoaDto>>(destino.IReadOnlyDictionaryPessoas);
		Assert.Equal("Ana", destino.IReadOnlyDictionaryPessoas["ana"].Nome);
		Assert.IsType<ReadOnlyDictionary<string, PessoaDto>>(destino.ReadOnlyDictionaryPessoas);
		Assert.Equal("Bruno", destino.ReadOnlyDictionaryPessoas["bruno"].Nome);
	}

	[Fact]
	public void Deve_Mapear_ConcurrentDictionary_E_ImmutableDictionary()
	{
		var mirror = new Mirror();
		var origem = new PessoaMapasOrigem
		{
			ConcurrentDictionaryPessoas = new ConcurrentDictionary<string, Pessoa>(
				new Dictionary<string, Pessoa>
				{
					["ana"] = new Pessoa { Id = 1, Nome = "Ana" }
				}),
			ImmutableDictionaryPessoas = ImmutableDictionary.CreateRange(
				new Dictionary<string, Pessoa>
				{
					["bruno"] = new Pessoa { Id = 2, Nome = "Bruno" }
				})
		};

		var destino = mirror.Reflect<PessoaMapasOrigem, PessoaMapasDestino>(origem);

		Assert.IsType<ConcurrentDictionary<string, PessoaDto>>(destino.ConcurrentDictionaryPessoas);
		Assert.Equal("Ana", destino.ConcurrentDictionaryPessoas["ana"].Nome);
		Assert.Equal(1, destino.ConcurrentDictionaryPessoas["ana"].Id);

		Assert.IsType<ImmutableDictionary<string, PessoaDto>>(destino.ImmutableDictionaryPessoas);
		Assert.Equal("Bruno", destino.ImmutableDictionaryPessoas["bruno"].Nome);
		Assert.Equal(2, destino.ImmutableDictionaryPessoas["bruno"].Id);
	}

	[Fact]
	public void Deve_Mapear_Hashtable_E_IDictionary_Nao_Genericos()
	{
		var mirror = new Mirror();
		var origem = new TelefoneMapasOrigem
		{
			HashtableTelefones = new Hashtable
			{
				[1] = "11999999999",
				[2] = "11888888888"
			},
			IDictionaryTelefones = new Hashtable
			{
				[3] = "11777777777"
			}
		};

		var destino = mirror.Reflect<TelefoneMapasOrigem, TelefoneMapasDestino>(origem);

		Assert.IsType<Hashtable>(destino.HashtableTelefones);
		Assert.Equal("11999999999", destino.HashtableTelefones[1]);
		Assert.Equal("11888888888", destino.HashtableTelefones[2]);

		Assert.IsType<Hashtable>(destino.IDictionaryTelefones);
		Assert.Equal("11777777777", destino.IDictionaryTelefones[3]);
	}

	[Fact]
	public void Deve_Mapear_Dictionary_Com_Chave_Complexa()
	{
		var mirror = new Mirror();
		var chave = new ChavePessoa { Id = 1, Nome = "Ana" };
		var origem = new MapaComChaveComplexaOrigem
		{
			PessoasPorChave = new Dictionary<ChavePessoa, string>
			{
				[chave] = "VIP"
			}
		};

		var destino = mirror.Reflect<MapaComChaveComplexaOrigem, MapaComChaveComplexaDestino>(origem);

		Assert.Single(destino.PessoasPorChave);
		var entrada = destino.PessoasPorChave.Single();
		Assert.Equal(1, entrada.Key.Id);
		Assert.Equal("Ana", entrada.Key.Nome);
		Assert.Equal("VIP", entrada.Value);
	}
}
