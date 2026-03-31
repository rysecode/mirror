using System.Collections;
using System.Collections.Concurrent;
using Mirror.Test.Models;
using System.Collections.ObjectModel;
using System.Collections.Immutable;

namespace Mirror.Test;

public class CollectionMappingTests
{
	[Fact]
	public void Deve_Mapear_Colecoes_De_Objetos_Para_Tipos_Comuns()
	{
		var mirror = new Mirror();
		var origem = new PessoaColecoesOrigem
		{
			ICollectionPessoas = CriarPessoas(),
			IReadOnlyListPessoas = CriarPessoas(),
			IEnumerablePessoas = CriarPessoas(),
			IListPessoas = CriarPessoas(),
			IReadOnlyCollectionPessoas = CriarPessoas(),
			ListPessoas = CriarPessoas().ToList(),
			HashSetPessoas = CriarPessoas().ToHashSet(),
			LinkedListPessoas = new LinkedList<Pessoa>(CriarPessoas()),
			CollectionPessoas = new Collection<Pessoa>(CriarPessoas().ToList()),
			ObservableCollectionPessoas = new ObservableCollection<Pessoa>(CriarPessoas()),
			ReadOnlyCollectionPessoas = new ReadOnlyCollection<Pessoa>(CriarPessoas().ToList()),
			ImmutableListPessoas = ImmutableList.CreateRange(CriarPessoas()),
			ConcurrentBagPessoas = new ConcurrentBag<Pessoa>(CriarPessoas()),
			QueuePessoas = new Queue<Pessoa>(CriarPessoas()),
			StackPessoas = new Stack<Pessoa>(CriarPessoas()),
			ArrayPessoas = CriarPessoas().ToArray()
		};

		var destino = mirror.Reflect<PessoaColecoesOrigem, PessoaColecoesDestino>(origem);

		ValidarColecaoDePessoas(destino.ICollectionPessoas, expectedType: typeof(List<PessoaDto>));
		ValidarColecaoDePessoas(destino.IReadOnlyListPessoas, expectedType: typeof(List<PessoaDto>));
		ValidarColecaoDePessoas(destino.IEnumerablePessoas, expectedType: typeof(List<PessoaDto>));
		ValidarColecaoDePessoas(destino.IListPessoas, expectedType: typeof(List<PessoaDto>));
		ValidarColecaoDePessoas(destino.IReadOnlyCollectionPessoas, expectedType: typeof(List<PessoaDto>));
		ValidarColecaoDePessoas(destino.ListPessoas, expectedType: typeof(List<PessoaDto>));
		ValidarColecaoDePessoas(destino.HashSetPessoas, expectedType: typeof(HashSet<PessoaDto>), preserveOrder: false);
		ValidarColecaoDePessoas(destino.LinkedListPessoas, expectedType: typeof(LinkedList<PessoaDto>));
		ValidarColecaoDePessoas(destino.CollectionPessoas, expectedType: typeof(Collection<PessoaDto>));
		ValidarColecaoDePessoas(destino.ObservableCollectionPessoas, expectedType: typeof(ObservableCollection<PessoaDto>));
		ValidarColecaoDePessoas(destino.ReadOnlyCollectionPessoas, expectedType: typeof(ReadOnlyCollection<PessoaDto>));
		ValidarColecaoDePessoas(destino.ImmutableListPessoas, expectedType: typeof(ImmutableList<PessoaDto>));
		ValidarColecaoDePessoas(destino.ConcurrentBagPessoas, expectedType: typeof(ConcurrentBag<PessoaDto>), preserveOrder: false);
		ValidarColecaoDePessoas(destino.QueuePessoas, expectedType: typeof(Queue<PessoaDto>));
		ValidarColecaoDePessoas(destino.StackPessoas, expectedType: typeof(Stack<PessoaDto>), preserveOrder: false);
		ValidarColecaoDePessoas(destino.ArrayPessoas, expectedType: typeof(PessoaDto[]));
	}

	[Fact]
	public void Deve_Mapear_Colecoes_De_Tipos_Simples_Para_Tipos_Comuns()
	{
		var mirror = new Mirror();
		var origem = new TelefoneColecoesOrigem
		{
			ICollectionTelefones = CriarTelefones(),
			IReadOnlyListTelefones = CriarTelefones(),
			IEnumerableTelefones = CriarTelefones(),
			IListTelefones = CriarTelefones(),
			IReadOnlyCollectionTelefones = CriarTelefones(),
			ListTelefones = CriarTelefones().ToList(),
			HashSetTelefones = CriarTelefones().ToHashSet(),
			LinkedListTelefones = new LinkedList<string>(CriarTelefones()),
			CollectionTelefones = new Collection<string>(CriarTelefones().ToList()),
			ObservableCollectionTelefones = new ObservableCollection<string>(CriarTelefones()),
			ReadOnlyCollectionTelefones = new ReadOnlyCollection<string>(CriarTelefones().ToList()),
			ImmutableListTelefones = ImmutableList.CreateRange(CriarTelefones()),
			ConcurrentBagTelefones = new ConcurrentBag<string>(CriarTelefones()),
			QueueTelefones = new Queue<string>(CriarTelefones()),
			StackTelefones = new Stack<string>(CriarTelefones()),
			ArrayTelefones = CriarTelefones().ToArray(),
			ArrayListTelefones = new ArrayList(CriarTelefones())
		};

		var destino = mirror.Reflect<TelefoneColecoesOrigem, TelefoneColecoesDestino>(origem);

		ValidarColecaoDeStrings(destino.ICollectionTelefones, expectedType: typeof(List<string>));
		ValidarColecaoDeStrings(destino.IReadOnlyListTelefones, expectedType: typeof(List<string>));
		ValidarColecaoDeStrings(destino.IEnumerableTelefones, expectedType: typeof(List<string>));
		ValidarColecaoDeStrings(destino.IListTelefones, expectedType: typeof(List<string>));
		ValidarColecaoDeStrings(destino.IReadOnlyCollectionTelefones, expectedType: typeof(List<string>));
		ValidarColecaoDeStrings(destino.ListTelefones, expectedType: typeof(List<string>));
		ValidarColecaoDeStrings(destino.HashSetTelefones, expectedType: typeof(HashSet<string>), preserveOrder: false);
		ValidarColecaoDeStrings(destino.LinkedListTelefones, expectedType: typeof(LinkedList<string>));
		ValidarColecaoDeStrings(destino.CollectionTelefones, expectedType: typeof(Collection<string>));
		ValidarColecaoDeStrings(destino.ObservableCollectionTelefones, expectedType: typeof(ObservableCollection<string>));
		ValidarColecaoDeStrings(destino.ReadOnlyCollectionTelefones, expectedType: typeof(ReadOnlyCollection<string>));
		ValidarColecaoDeStrings(destino.ImmutableListTelefones, expectedType: typeof(ImmutableList<string>));
		ValidarColecaoDeStrings(destino.ConcurrentBagTelefones, expectedType: typeof(ConcurrentBag<string>), preserveOrder: false);
		ValidarColecaoDeStrings(destino.QueueTelefones, expectedType: typeof(Queue<string>));
		ValidarColecaoDeStrings(destino.StackTelefones, expectedType: typeof(Stack<string>), preserveOrder: false);
		ValidarColecaoDeStrings(destino.ArrayTelefones, expectedType: typeof(string[]));
		ValidarArrayList(destino.ArrayListTelefones);
	}

	private static List<Pessoa> CriarPessoas()
	{
		return
		[
			new Pessoa { Id = 1, Nome = "Ana", Email = "ana@teste.com" },
			new Pessoa { Id = 2, Nome = "Bruno", Email = "bruno@teste.com" }
		];
	}

	private static List<string> CriarTelefones()
	{
		return
		[
			"11999999999",
			"11888888888"
		];
	}

	private static void ValidarColecaoDePessoas(IEnumerable<PessoaDto> colecao, Type expectedType, bool preserveOrder = true)
	{
		Assert.NotNull(colecao);
		Assert.IsType(expectedType, colecao);

		var itens = colecao.ToList();
		Assert.Equal(2, itens.Count);

		if (preserveOrder)
		{
			Assert.Equal(1, itens[0].Id);
			Assert.Equal("Ana", itens[0].Nome);
			Assert.Equal(2, itens[1].Id);
			Assert.Equal("Bruno", itens[1].Nome);
			return;
		}

		Assert.Contains(itens, p => p.Id == 1 && p.Nome == "Ana");
		Assert.Contains(itens, p => p.Id == 2 && p.Nome == "Bruno");
	}

	private static void ValidarColecaoDeStrings(IEnumerable<string> colecao, Type expectedType, bool preserveOrder = true)
	{
		Assert.NotNull(colecao);
		Assert.IsType(expectedType, colecao);

		var itens = colecao.ToList();
		Assert.Equal(2, itens.Count);

		if (preserveOrder)
		{
			Assert.Equal("11999999999", itens[0]);
			Assert.Equal("11888888888", itens[1]);
			return;
		}

		Assert.Contains("11999999999", itens);
		Assert.Contains("11888888888", itens);
	}

	private static void ValidarArrayList(ArrayList colecao)
	{
		Assert.NotNull(colecao);
		Assert.IsType<ArrayList>(colecao);
		Assert.Equal(2, colecao.Count);
		Assert.Equal("11999999999", colecao[0]);
		Assert.Equal("11888888888", colecao[1]);
	}
}
