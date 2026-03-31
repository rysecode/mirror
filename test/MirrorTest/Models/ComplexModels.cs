using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Immutable;

namespace Mirror.Test.Models;

// Modelos para teste de objetos complexos
public class Endereco
{
	public string Logradouro { get; set; } = string.Empty;
	public string Numero { get; set; } = string.Empty;
	public string Cep { get; set; } = string.Empty;
	public string Cidade { get; set; } = string.Empty;
	public string? Complemento { get; set; }
}

public class EnderecoDto
{
	public string Logradouro { get; set; } = string.Empty;
	public string Numero { get; set; } = string.Empty;
	public string Cep { get; set; } = string.Empty;
	public string Cidade { get; set; } = string.Empty;
	public string? Complemento { get; set; }
}

public class Cliente
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public Endereco? Endereco { get; set; }
	public Endereco EnderecoPrincipal { get; set; } = new();
	public List<Endereco> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

public class ClienteDto
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public Endereco? Endereco { get; set; }
	public EnderecoDto EnderecoPrincipal { get; set; } = new();
	public List<EnderecoDto> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

// Modelos específicos do seu caso
// Versão do EmpresaRequest com construtor público (para testes com new())
public class EmpresaRequest
{
	public string RazaoSocial { get; set; } = string.Empty;
	public string Fantasia { get; set; } = string.Empty;
	public string Cnpj { get; set; } = string.Empty;

	public EmpresaRequest() { }

	public static EmpresaRequest Create(string razaoSocial, string fantasia, string cnpj)
	{
		return new EmpresaRequest
		{
			RazaoSocial = razaoSocial,
			Fantasia = fantasia,
			Cnpj = cnpj
		};
	}
}

public class EmpresaRequestFactory
{
	public string RazaoSocial { get; set; } = string.Empty;
	public string Fantasia { get; set; } = string.Empty;
	public string Cnpj { get; set; } = string.Empty;

	private EmpresaRequestFactory() { }

	public static EmpresaRequestFactory Create(string razaoSocial, string fantasia, string cnpj)
	{
		return new EmpresaRequestFactory
		{
			RazaoSocial = razaoSocial,
			Fantasia = fantasia,
			Cnpj = cnpj,
		};
	}
}

public class EmpresaRequestComValidacao
{
	public string RazaoSocial { get; set; }
	public string Fantasia { get; set; }
	public string Cnpj { get; set; }

	private EmpresaRequestComValidacao(string razaoSocial, string fantasia, string cnpj)
	{
		RazaoSocial = razaoSocial;
		Fantasia = fantasia;
		Cnpj = cnpj;
	}

	public static EmpresaRequestComValidacao Create(string razaoSocial, string fantasia, string cnpj)
	{
		if (string.IsNullOrWhiteSpace(razaoSocial))
			throw new ArgumentException("Razão social é obrigatória");

		if (string.IsNullOrWhiteSpace(fantasia))
			throw new ArgumentException("Fantasia é obrigatória");

		if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14)
			throw new ArgumentException("CNPJ inválido");

		return new EmpresaRequestComValidacao(razaoSocial, fantasia, cnpj);
	}
}

public class CreateEmpresaRequestDto
{
	public string RazaoSocial { get; set; } = string.Empty;
	public string Fantasia { get; set; } = string.Empty;
	public string Cnpj { get; set; } = string.Empty;
}

public class KycEnterpriseRequest
{
	public Guid ProvisionamentoId { get; set; }
	public EmpresaRequest Empresa { get; set; } = new();
	public List<Endereco> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

public class KycEnterpriseRequestDto
{
	public Guid ProvisionamentoId { get; set; }
	public CreateEmpresaRequestDto Empresa { get; set; } = new();
	public List<EnderecoDto> Enderecos { get; set; } = new();
	public List<string> Telefones { get; set; } = new();
}

public class Node
{
	public string Nome { get; set; } = string.Empty;
	public Node? Filho { get; set; }
}

public class NodeDto
{
	public string Nome { get; set; } = string.Empty;
	public NodeDto? Filho { get; set; }
}

public class PessoaColecoesOrigem
{
	public ICollection<Pessoa> ICollectionPessoas { get; set; } = new List<Pessoa>();
	public IReadOnlyList<Pessoa> IReadOnlyListPessoas { get; set; } = new List<Pessoa>();
	public IEnumerable<Pessoa> IEnumerablePessoas { get; set; } = new List<Pessoa>();
	public IList<Pessoa> IListPessoas { get; set; } = new List<Pessoa>();
	public IReadOnlyCollection<Pessoa> IReadOnlyCollectionPessoas { get; set; } = new List<Pessoa>();
	public List<Pessoa> ListPessoas { get; set; } = new();
	public HashSet<Pessoa> HashSetPessoas { get; set; } = new();
	public LinkedList<Pessoa> LinkedListPessoas { get; set; } = new();
	public Collection<Pessoa> CollectionPessoas { get; set; } = new();
	public ObservableCollection<Pessoa> ObservableCollectionPessoas { get; set; } = new();
	public ReadOnlyCollection<Pessoa> ReadOnlyCollectionPessoas { get; set; } = new(new List<Pessoa>());
	public ImmutableList<Pessoa> ImmutableListPessoas { get; set; } = ImmutableList<Pessoa>.Empty;
	public ConcurrentBag<Pessoa> ConcurrentBagPessoas { get; set; } = new();
	public Queue<Pessoa> QueuePessoas { get; set; } = new();
	public Stack<Pessoa> StackPessoas { get; set; } = new();
	public Pessoa[] ArrayPessoas { get; set; } = [];
}

public class PessoaColecoesDestino
{
	public ICollection<PessoaDto> ICollectionPessoas { get; set; } = new List<PessoaDto>();
	public IReadOnlyList<PessoaDto> IReadOnlyListPessoas { get; set; } = new List<PessoaDto>();
	public IEnumerable<PessoaDto> IEnumerablePessoas { get; set; } = new List<PessoaDto>();
	public IList<PessoaDto> IListPessoas { get; set; } = new List<PessoaDto>();
	public IReadOnlyCollection<PessoaDto> IReadOnlyCollectionPessoas { get; set; } = new List<PessoaDto>();
	public List<PessoaDto> ListPessoas { get; set; } = new();
	public HashSet<PessoaDto> HashSetPessoas { get; set; } = new();
	public LinkedList<PessoaDto> LinkedListPessoas { get; set; } = new();
	public Collection<PessoaDto> CollectionPessoas { get; set; } = new();
	public ObservableCollection<PessoaDto> ObservableCollectionPessoas { get; set; } = new();
	public ReadOnlyCollection<PessoaDto> ReadOnlyCollectionPessoas { get; set; } = new(new List<PessoaDto>());
	public ImmutableList<PessoaDto> ImmutableListPessoas { get; set; } = ImmutableList<PessoaDto>.Empty;
	public ConcurrentBag<PessoaDto> ConcurrentBagPessoas { get; set; } = new();
	public Queue<PessoaDto> QueuePessoas { get; set; } = new();
	public Stack<PessoaDto> StackPessoas { get; set; } = new();
	public PessoaDto[] ArrayPessoas { get; set; } = [];
}

public class TelefoneColecoesOrigem
{
	public ICollection<string> ICollectionTelefones { get; set; } = new List<string>();
	public IReadOnlyList<string> IReadOnlyListTelefones { get; set; } = new List<string>();
	public IEnumerable<string> IEnumerableTelefones { get; set; } = new List<string>();
	public IList<string> IListTelefones { get; set; } = new List<string>();
	public IReadOnlyCollection<string> IReadOnlyCollectionTelefones { get; set; } = new List<string>();
	public List<string> ListTelefones { get; set; } = new();
	public HashSet<string> HashSetTelefones { get; set; } = new();
	public LinkedList<string> LinkedListTelefones { get; set; } = new();
	public Collection<string> CollectionTelefones { get; set; } = new();
	public ObservableCollection<string> ObservableCollectionTelefones { get; set; } = new();
	public ReadOnlyCollection<string> ReadOnlyCollectionTelefones { get; set; } = new(new List<string>());
	public ImmutableList<string> ImmutableListTelefones { get; set; } = ImmutableList<string>.Empty;
	public ConcurrentBag<string> ConcurrentBagTelefones { get; set; } = new();
	public Queue<string> QueueTelefones { get; set; } = new();
	public Stack<string> StackTelefones { get; set; } = new();
	public string[] ArrayTelefones { get; set; } = [];
	public ArrayList ArrayListTelefones { get; set; } = new();
}

public class TelefoneColecoesDestino
{
	public ICollection<string> ICollectionTelefones { get; set; } = new List<string>();
	public IReadOnlyList<string> IReadOnlyListTelefones { get; set; } = new List<string>();
	public IEnumerable<string> IEnumerableTelefones { get; set; } = new List<string>();
	public IList<string> IListTelefones { get; set; } = new List<string>();
	public IReadOnlyCollection<string> IReadOnlyCollectionTelefones { get; set; } = new List<string>();
	public List<string> ListTelefones { get; set; } = new();
	public HashSet<string> HashSetTelefones { get; set; } = new();
	public LinkedList<string> LinkedListTelefones { get; set; } = new();
	public Collection<string> CollectionTelefones { get; set; } = new();
	public ObservableCollection<string> ObservableCollectionTelefones { get; set; } = new();
	public ReadOnlyCollection<string> ReadOnlyCollectionTelefones { get; set; } = new(new List<string>());
	public ImmutableList<string> ImmutableListTelefones { get; set; } = ImmutableList<string>.Empty;
	public ConcurrentBag<string> ConcurrentBagTelefones { get; set; } = new();
	public Queue<string> QueueTelefones { get; set; } = new();
	public Stack<string> StackTelefones { get; set; } = new();
	public string[] ArrayTelefones { get; set; } = [];
	public ArrayList ArrayListTelefones { get; set; } = new();
}

public class PessoaMapasOrigem
{
	public Dictionary<string, Pessoa> DictionaryPessoas { get; set; } = new();
	public SortedList<string, Pessoa> SortedListPessoas { get; set; } = new();
	public IReadOnlyDictionary<string, Pessoa> IReadOnlyDictionaryPessoas { get; set; } = new Dictionary<string, Pessoa>();
	public ReadOnlyDictionary<string, Pessoa> ReadOnlyDictionaryPessoas { get; set; } = new(new Dictionary<string, Pessoa>());
	public ConcurrentDictionary<string, Pessoa> ConcurrentDictionaryPessoas { get; set; } = new();
	public ImmutableDictionary<string, Pessoa> ImmutableDictionaryPessoas { get; set; } = ImmutableDictionary<string, Pessoa>.Empty;
}

public class PessoaMapasDestino
{
	public Dictionary<string, PessoaDto> DictionaryPessoas { get; set; } = new();
	public SortedList<string, PessoaDto> SortedListPessoas { get; set; } = new();
	public IReadOnlyDictionary<string, PessoaDto> IReadOnlyDictionaryPessoas { get; set; } = new Dictionary<string, PessoaDto>();
	public ReadOnlyDictionary<string, PessoaDto> ReadOnlyDictionaryPessoas { get; set; } = new(new Dictionary<string, PessoaDto>());
	public ConcurrentDictionary<string, PessoaDto> ConcurrentDictionaryPessoas { get; set; } = new();
	public ImmutableDictionary<string, PessoaDto> ImmutableDictionaryPessoas { get; set; } = ImmutableDictionary<string, PessoaDto>.Empty;
}

public class TelefoneMapasOrigem
{
	public Dictionary<int, string> DictionaryTelefones { get; set; } = new();
	public SortedList<int, string> SortedListTelefones { get; set; } = new();
	public Hashtable HashtableTelefones { get; set; } = new();
	public IDictionary IDictionaryTelefones { get; set; } = new Hashtable();
}

public class TelefoneMapasDestino
{
	public Dictionary<int, string> DictionaryTelefones { get; set; } = new();
	public SortedList<int, string> SortedListTelefones { get; set; } = new();
	public Hashtable HashtableTelefones { get; set; } = new();
	public IDictionary IDictionaryTelefones { get; set; } = new Hashtable();
}

public class ChavePessoa
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;

	public override bool Equals(object? obj)
	{
		return obj is ChavePessoa other &&
			Id == other.Id &&
			string.Equals(Nome, other.Nome, StringComparison.Ordinal);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Nome);
	}
}

public class ChavePessoaDto
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;

	public override bool Equals(object? obj)
	{
		return obj is ChavePessoaDto other &&
			Id == other.Id &&
			string.Equals(Nome, other.Nome, StringComparison.Ordinal);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Nome);
	}
}

public class MapaComChaveComplexaOrigem
{
	public Dictionary<ChavePessoa, string> PessoasPorChave { get; set; } = new();
}

public class MapaComChaveComplexaDestino
{
	public Dictionary<ChavePessoaDto, string> PessoasPorChave { get; set; } = new();
}

public class ContatoDetalhado
{
	public string Tipo { get; set; } = string.Empty;
	public string Valor { get; set; } = string.Empty;
}

public class ContatoDetalhadoDto
{
	public string Tipo { get; set; } = string.Empty;
	public string Valor { get; set; } = string.Empty;
}

public class PessoaDetalhada
{
	public string Nome { get; set; } = string.Empty;
	public List<string> Tags { get; set; } = new();
	public List<Endereco> Enderecos { get; set; } = new();
	public List<ContatoDetalhado> Contatos { get; set; } = new();
}

public class PessoaDetalhadaDto
{
	public string Nome { get; set; } = string.Empty;
	public List<string> Tags { get; set; } = new();
	public List<EnderecoDto> Enderecos { get; set; } = new();
	public List<ContatoDetalhadoDto> Contatos { get; set; } = new();
}

public class ClienteProfundo
{
	public int Id { get; set; }
	public PessoaDetalhada Pessoa { get; set; } = new();
}

public class ClienteProfundoDto
{
	public int Id { get; set; }
	public PessoaDetalhadaDto Pessoa { get; set; } = new();
}

public class PreferenciaCanal
{
	public string Canal { get; set; } = string.Empty;
	public bool Ativo { get; set; }
}

public class PreferenciaCanalDto
{
	public string Canal { get; set; } = string.Empty;
	public bool Ativo { get; set; }
}

public class PessoaDetalhadaComMapas
{
	public string Nome { get; set; } = string.Empty;
	public Endereco? EnderecoPrincipal { get; set; }
	public List<string> Tags { get; set; } = new();
	public List<Endereco?> Enderecos { get; set; } = new();
	public List<ContatoDetalhado?> Contatos { get; set; } = new();
	public Dictionary<string, Endereco?> EnderecosPorTipo { get; set; } = new();
	public Dictionary<string, List<ContatoDetalhado?>> ContatosPorCategoria { get; set; } = new();
	public SortedList<string, PreferenciaCanal?> Preferencias { get; set; } = new();
}

public class PessoaDetalhadaComMapasDto
{
	public string Nome { get; set; } = string.Empty;
	public EnderecoDto? EnderecoPrincipal { get; set; }
	public List<string> Tags { get; set; } = new();
	public List<EnderecoDto?> Enderecos { get; set; } = new();
	public List<ContatoDetalhadoDto?> Contatos { get; set; } = new();
	public Dictionary<string, EnderecoDto?> EnderecosPorTipo { get; set; } = new();
	public Dictionary<string, List<ContatoDetalhadoDto?>> ContatosPorCategoria { get; set; } = new();
	public SortedList<string, PreferenciaCanalDto?> Preferencias { get; set; } = new();
}

public class ClienteHibrido
{
	public int Id { get; set; }
	public string? Observacao { get; set; }
	public PessoaDetalhadaComMapas? Pessoa { get; set; }
}

public class ClienteHibridoDto
{
	public int Id { get; set; }
	public string? Observacao { get; set; }
	public PessoaDetalhadaComMapasDto? Pessoa { get; set; }
}
