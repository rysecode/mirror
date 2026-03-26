# Mirror - Biblioteca de Mapeamento de Objetos para .NET

## 📋 Índice
- [Visão Geral](#visão-geral)
- [Instalação](#instalação)
- [Conceitos Fundamentais](#conceitos-fundamentais)
- [Guia de Uso](#guia-de-uso)
- [Profiles e Configuração](#profiles-e-configuração)
- [Injeção de Dependência](#injeção-de-dependência)
- [Exemplos Práticos](#exemplos-práticos)
- [Melhores Práticas](#melhores-práticas)
- [API Reference](#api-reference)
- [Contribuição](#contribuição)

## Visão Geral

### O que é o Mirror?

Mirror é uma biblioteca leve de mapeamento objeto-objeto para .NET, criada como uma alternativa simples e intuitiva ao AutoMapper. O nome "Mirror" reflete perfeitamente seu propósito: espelhar/refletir propriedades de um objeto para outro, como se fosse um espelho.

### Objetivos do Projeto

- **Simplicidade**: API intuitiva e fácil de aprender
- **Performance**: Mínimo overhead com cache de expressões
- **Flexibilidade**: Suporte a transformações customizadas
- **Integração**: Fácil integração com DI container do .NET
- **Leveza**: Sem dependências externas desnecessárias

### Principais Características

- ✅ Mapeamento automático por convenção (nome/tipo)
- ✅ Suporte a tipos anuláveis (Nullable)
- ✅ Transformações customizadas
- ✅ Sistema de Profiles para organização
- ✅ Cache de reflexão para performance
- ✅ Injeção de dependência nativa
- ✅ Suporte a coleções (IEnumerable)
- ✅ Tratamento de exceções específico
- ✅ API fluente e expressiva

## Instalação

### Via NuGet Package Manager
```powershell
Install-Package Mirror
```

### Via .NET CLI
```bash
dotnet add package Mirror
```

### Via Package Reference
```xml
<PackageReference Include="Mirror" Version="1.0.0" />
```

## Conceitos Fundamentais

### Como Funciona?

O Mirror utiliza reflexão e express trees para copiar propriedades entre objetos. O processo básico é:

1. **Análise**: Examina as propriedades públicas da origem e destino
2. **Correspondência**: Compara por nome e tipo compatível
3. **Cache**: Armazena os mapeamentos para operações futuras
4. **Transformação**: Aplica regras customizadas quando configurado
5. **Execução**: Copia os valores de origem para destino

### Terminologia

- **Reflection/Reflect**: Processo de espelhar/mapear objetos
- **Origem**: Objeto de onde os dados são lidos
- **Destino**: Objeto onde os dados são escritos
- **Profile**: Classe que agrupa configurações de mapeamento
- **Transformação**: Regra customizada para modificar valores

## Guia de Uso

### 1. Mapeamento Básico

```csharp
using Mirror;

// Classes de exemplo
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public DateTime DataNascimento { get; set; }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
}

// Uso básico
var mirror = new Mirror.Mirror();
var usuario = new Usuario 
{ 
    Id = 1, 
    Nome = "João", 
    Email = "joao@email.com",
    DataNascimento = DateTime.Now
};

var usuarioDto = mirror.Reflect<Usuario, UsuarioDto>(usuario);
```

### 2. Mapeamento com Objeto Existente

```csharp
// Reutilizando um objeto destino existente
var usuarioDto = new UsuarioDto();
mirror.Reflect(usuario, usuarioDto);
```

### 3. Mapeamento de Coleções

```csharp
var usuarios = new List<Usuario>
{
    new Usuario { Id = 1, Nome = "João" },
    new Usuario { Id = 2, Nome = "Maria" }
};

// Usando método de extensão
var usuariosDto = mirror.ReflectAll<Usuario, UsuarioDto>(usuarios);

// Ou manualmente com LINQ
var usuariosDto = usuarios.Select(u => mirror.Reflect<Usuario, UsuarioDto>(u));
```

### 4. Transformações Customizadas

```csharp
var config = new MirrorConfiguration();

config.CreateReflection<Usuario, UsuarioDto>()
    .ForMember(dto => dto.Nome, u => u.Nome.ToUpper())
    .ForMember(dto => dto.Email, u => u.Email.ToLower());

var mirror = new Mirror.Mirror(config);
var resultado = mirror.Reflect<Usuario, UsuarioDto>(usuario);
```

### 5. Métodos de Extensão

```csharp
using Mirror.Extensions;

// Extensão direta no objeto
var usuarioDto = usuario.ReflectTo<UsuarioDto>(mirror);

// Para coleções
var usuariosDto = usuarios.ReflectAllTo<UsuarioDto>(mirror);
```

## Profiles e Configuração

### Criando um Profile

```csharp
using Mirror;

public class UsuarioProfile : MirrorProfile
{
    public override void Configure(IMirrorProfileExpression expression)
    {
        expression.CreateReflection<Usuario, UsuarioDto>()
            .ForMember<Usuario, UsuarioDto, string>(
                dto => dto.NomeCompleto, 
                u => $"{u.PrimeiroNome} {u.UltimoNome}"
            )
            .ForMember<Usuario, UsuarioDto, string>(
                dto => dto.Status, 
                u => u.Ativo ? "Ativo" : "Inativo"
            )
            .ForMember<Usuario, UsuarioDto, string>(
                dto => dto.Idade,
                u => CalcularIdade(u.DataNascimento)
            );

        expression.CreateReflection<Usuario, UsuarioListaDto>()
            .ForMember<Usuario, UsuarioListaDto, string>(
                dto => dto.Nome, 
                u => u.PrimeiroNome
            );
    }

    private int CalcularIdade(DateTime dataNascimento)
    {
        var hoje = DateTime.Today;
        var idade = hoje.Year - dataNascimento.Year;
        if (dataNascimento.Date > hoje.AddYears(-idade)) idade--;
        return idade;
    }
}

// Profile para Produto
public class ProdutoProfile : MirrorProfile
{
    public override void Configure(IMirrorProfileExpression expression)
    {
        expression.CreateReflection<Produto, ProdutoDto>()
            .ForMember<Produto, ProdutoDto, string>(
                dto => dto.PrecoFormatado, 
                p => p.Preco.ToString("C2")
            )
            .ForMember<Produto, ProdutoDto, string>(
                dto => dto.Disponibilidade, 
                p => p.EmEstoque ? "Em estoque" : "Fora de estoque"
            );
    }
}
```

### Configuração Múltipla de Profiles

```csharp
public class ApplicationProfile : MirrorProfile
{
    public override void Configure(IMirrorProfileExpression expression)
    {
        // Usuário mappings
        expression.CreateReflection<Usuario, UsuarioDto>()
            .ForMember(dto => dto.NomeCompleto, u => $"{u.PrimeiroNome} {u.UltimoNome}");

        // Produto mappings
        expression.CreateReflection<Produto, ProdutoDto>()
            .ForMember(dto => dto.PrecoFormatado, p => p.Preco.ToString("C2"));

        // Pedido mappings
        expression.CreateReflection<Pedido, PedidoDto>()
            .ForMember(dto => dto.ValorTotal, p => p.Itens.Sum(i => i.Valor));
    }
}
```

## Injeção de Dependência

### Configuração no Program.cs (Minimal API)

```csharp
using Mirror;
using Mirror.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configuração simples
builder.Services.AddMirror();

// Configuração com profiles
builder.Services.AddMirrorWithProfiles(
    configure: config =>
    {
        // Configurações globais
        config.SetMaxDepth(5);
        config.IgnoreNullValues(true);
    },
    assemblies: Assembly.GetExecutingAssembly()
);

// Ou configuração separada
builder.Services.AddMirror(config =>
{
    config.ConfigureProfiles(profiles =>
    {
        profiles.AddProfile<UsuarioProfile>();
        profiles.AddProfile<ProdutoProfile>();
    });
});

var app = builder.Build();

// Aplica os profiles (opcional - geralmente automático)
var profileApplier = app.Services.GetRequiredService<MirrorProfileApplier>();
profileApplier.ApplyProfiles();

app.MapGet("/usuario/{id}", async (int id, IMirror mirror) =>
{
    var usuario = await GetUsuario(id);
    var usuarioDto = mirror.Reflect<Usuario, UsuarioDto>(usuario);
    return Results.Ok(usuarioDto);
});

app.Run();
```

### Configuração em Controllers Tradicionais

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IMirror _mirror;
    private readonly IUsuarioRepository _repository;

    public UsuariosController(IMirror mirror, IUsuarioRepository repository)
    {
        _mirror = mirror;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _repository.GetAllAsync();
        var usuariosDto = _mirror.ReflectAll<Usuario, UsuarioListaDto>(usuarios);
        return Ok(usuariosDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario == null)
            return NotFound();

        var usuarioDto = _mirror.Reflect<Usuario, UsuarioDto>(usuario);
        return Ok(usuarioDto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UsuarioCreateDto createDto)
    {
        var usuario = _mirror.Reflect<UsuarioCreateDto, Usuario>(createDto);
        await _repository.AddAsync(usuario);
        
        var usuarioDto = _mirror.Reflect<Usuario, UsuarioDto>(usuario);
        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuarioDto);
    }
}
```

## Exemplos Práticos

### Exemplo 1: Sistema de E-commerce

```csharp
// Models
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public decimal Preco { get; set; }
    public int QuantidadeEstoque { get; set; }
    public DateTime DataCadastro { get; set; }
    public bool Ativo { get; set; }
    public Categoria Categoria { get; set; }
}

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
}

// DTOs
public class ProdutoListaDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string PrecoFormatado { get; set; }
    public string StatusEstoque { get; set; }
    public string Categoria { get; set; }
}

public class ProdutoDetalheDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public decimal Preco { get; set; }
    public string PrecoFormatado { get; set; }
    public int QuantidadeEstoque { get; set; }
    public string Status { get; set; }
    public string DataCadastroFormatada { get; set; }
    public CategoriaDto Categoria { get; set; }
}

public class CategoriaDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
}

// Profile
public class EcommerceProfile : MirrorProfile
{
    public override void Configure(IMirrorProfileExpression expression)
    {
        expression.CreateReflection<Produto, ProdutoListaDto>()
            .ForMember(dto => dto.PrecoFormatado, p => p.Preco.ToString("C2"))
            .ForMember(dto => dto.StatusEstoque, p => 
                p.QuantidadeEstoque > 0 ? $"Em estoque ({p.QuantidadeEstoque})" : "Fora de estoque")
            .ForMember(dto => dto.Categoria, p => p.Categoria?.Nome ?? "Sem categoria");

        expression.CreateReflection<Produto, ProdutoDetalheDto>()
            .ForMember(dto => dto.PrecoFormatado, p => p.Preco.ToString("C2"))
            .ForMember(dto => dto.Status, p => p.Ativo ? "Ativo" : "Inativo")
            .ForMember(dto => dto.DataCadastroFormatada, 
                p => p.DataCadastro.ToString("dd/MM/yyyy HH:mm"));

        expression.CreateReflection<Categoria, CategoriaDto>();
    }
}

// Service
public class ProdutoService
{
    private readonly IMirror _mirror;
    private readonly IProdutoRepository _repository;

    public ProdutoService(IMirror mirror, IProdutoRepository repository)
    {
        _mirror = mirror;
        _repository = repository;
    }

    public async Task<IEnumerable<ProdutoListaDto>> GetProdutosEmDestaque()
    {
        var produtos = await _repository.GetProdutosEmDestaque();
        return _mirror.ReflectAll<Produto, ProdutoListaDto>(produtos);
    }

    public async Task<ProdutoDetalheDto> GetProdutoDetalhe(int id)
    {
        var produto = await _repository.GetByIdWithCategoria(id);
        return _mirror.Reflect<Produto, ProdutoDetalheDto>(produto);
    }
}
```

### Exemplo 2: Sistema de Pedidos

```csharp
public class Pedido
{
    public int Id { get; set; }
    public string Codigo { get; set; }
    public DateTime DataPedido { get; set; }
    public Cliente Cliente { get; set; }
    public List<ItemPedido> Itens { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; }
}

public class PedidoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; }
    public string DataFormatada { get; set; }
    public string ClienteNome { get; set; }
    public int QuantidadeItens { get; set; }
    public string ValorTotalFormatado { get; set; }
    public string Status { get; set; }
    public string StatusCor { get; set; }
}

public class PedidoProfile : MirrorProfile
{
    public override void Configure(IMirrorProfileExpression expression)
    {
        expression.CreateReflection<Pedido, PedidoDto>()
            .ForMember(dto => dto.DataFormatada, 
                p => p.DataPedido.ToString("dd/MM/yyyy"))
            .ForMember(dto => dto.ClienteNome, 
                p => p.Cliente?.Nome ?? "Cliente não informado")
            .ForMember(dto => dto.QuantidadeItens, 
                p => p.Itens?.Sum(i => i.Quantidade) ?? 0)
            .ForMember(dto => dto.ValorTotalFormatado, 
                p => p.ValorTotal.ToString("C2"))
            .ForMember(dto => dto.StatusCor, p => 
                p.Status switch
                {
                    "Aguardando" => "yellow",
                    "Pago" => "green",
                    "Enviado" => "blue",
                    "Entregue" => "green",
                    "Cancelado" => "red",
                    _ => "gray"
                });
    }
}
```

## Melhores Práticas

### 1. Organização de Profiles

```csharp
// ✅ CORRETO: Um profile por módulo/contexto
public class UsuarioProfile : MirrorProfile { }
public class ProdutoProfile : MirrorProfile { }
public class PedidoProfile : MirrorProfile { }

// ❌ ERRADO: Tudo em um único profile gigante
public class TodosMappingsProfile : MirrorProfile { }
```

### 2. Nomenclatura Consistente

```csharp
// ✅ CORRETO: Nomes claros e consistentes
public class UsuarioCreateDto { }
public class UsuarioUpdateDto { }
public class UsuarioListaDto { }
public class UsuarioDetalheDto { }

// ❌ ERRADO: Nomes confusos
public class UsuarioDTO1 { }
public class Usuario2 { }
```

### 3. Tratamento de Erros

```csharp
public class UsuarioService
{
    private readonly IMirror _mirror;
    private readonly ILogger<UsuarioService> _logger;

    public async Task<UsuarioDto> GetUsuario(int id)
    {
        try
        {
            var usuario = await _repository.GetByIdAsync(id);
            return _mirror.Reflect<Usuario, UsuarioDto>(usuario);
        }
        catch (MirrorException ex)
        {
            _logger.LogError(ex, "Erro ao mapear usuário {Id}", id);
            throw new ApplicationException("Erro ao processar usuário", ex);
        }
    }
}
```

### 4. Performance

```csharp
// ✅ CORRETO: Reutilizar instância do Mirror
public class MeuService
{
    private readonly IMirror _mirror;
    
    public MeuService(IMirror mirror)
    {
        _mirror = mirror; // Injetado como singleton/scoped
    }
}

// ❌ ERRADO: Criar nova instância a cada chamada
public void Metodo()
{
    var mirror = new Mirror.Mirror(); // Evite!
}
```

### 5. Validação de Dados

```csharp
public class UsuarioValidator
{
    private readonly IMirror _mirror;

    public async Task<Usuario> CreateUsuario(UsuarioCreateDto dto)
    {
        // Validar DTO antes do mapeamento
        if (string.IsNullOrEmpty(dto.Email))
            throw new ValidationException("Email é obrigatório");

        var usuario = _mirror.Reflect<UsuarioCreateDto, Usuario>(dto);
        
        // Validar entidade após mapeamento
        if (await _repository.EmailExists(usuario.Email))
            throw new ValidationException("Email já existe");

        return usuario;
    }
}
```

### 6. Testes Unitários

```csharp
public class UsuarioProfileTests
{
    [Fact]
    public void Deve_Mapear_Usuario_Para_Dto_Corretamente()
    {
        // Arrange
        var config = new MirrorConfiguration();
        var profile = new UsuarioProfile();
        profile.Configure(new MirrorProfileExpression(config));
        
        var mirror = new Mirror.Mirror(config);
        var usuario = new Usuario 
        { 
            PrimeiroNome = "João", 
            UltimoNome = "Silva",
            Ativo = true,
            DataNascimento = new DateTime(1990, 1, 1)
        };

        // Act
        var dto = mirror.Reflect<Usuario, UsuarioDto>(usuario);

        // Assert
        Assert.Equal("João Silva", dto.NomeCompleto);
        Assert.Equal("Ativo", dto.Status);
        Assert.Equal(DateTime.Today.Year - 1990, dto.Idade);
    }

    [Fact]
    public void Deve_Ignorar_Propriedades_Sem_Correspondencia()
    {
        var mirror = new Mirror.Mirror();
        var origem = new { Id = 1, Nome = "Teste" };
        
        var destino = new Destino { Id = 0, Descricao = "Original" };
        mirror.Reflect(origem, destino);
        
        Assert.Equal(1, destino.Id); // Mapeado
        Assert.Equal("Original", destino.Descricao); // Mantido
    }

    public class Destino
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
    }
}
```

### 7. Configuração por Atributos (Opcional)

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class MirrorIgnoreAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class MirrorMapAttribute : Attribute
{
    public string TargetProperty { get; }
    public MirrorMapAttribute(string targetProperty) => TargetProperty = targetProperty;
}

// Uso
public class Usuario
{
    public int Id { get; set; }
    
    [MirrorMap("NomeCompleto")]
    public string Nome { get; set; }
    
    [MirrorIgnore]
    public string Senha { get; set; }
}
```

## API Reference

### IMirror

| Método | Descrição |
|--------|-----------|
| `TDestino Reflect<TOrigem, TDestino>(TOrigem origem)` | Cria novo objeto destino mapeado da origem |
| `void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino)` | Mapeia origem para objeto destino existente |

### MirrorConfiguration

| Método | Descrição |
|--------|-----------|
| `CreateReflection<TOrigem, TDestino>()` | Inicia configuração de mapeamento |
| `SetMaxDepth(int depth)` | Define profundidade máxima para objetos aninhados |
| `IgnoreNullValues(bool ignore)` | Configura se valores nulos são ignorados |
| `EnableValidation(bool enable)` | Habilita validação de tipos |

### IReflectionExpression

| Método | Descrição |
|--------|-----------|
| `ForMember<TProp>(destinoMember, transform)` | Configura transformação para propriedade específica |

### Métodos de Extensão

| Método | Descrição |
|--------|-----------|
| `ReflectAll<TOrigem, TDestino>(this IMirror, IEnumerable<TOrigem>)` | Mapeia coleção de objetos |
| `ReflectTo<TDestino>(this object, IMirror)` | Extensão direta no objeto |

## Contribuição

### Como Contribuir

1. Fork o projeto
2. Crie uma branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

### Diretrizes de Código

- Mantenha o código limpo e documentado
- Siga os princípios SOLID
- Escreva testes para novas funcionalidades
- Atualize a documentação quando necessário

### Reportando Issues

Use o GitHub Issues para reportar:
- Bugs
- Sugestões de melhoria
- Dúvidas

## Conclusão

Mirror é uma biblioteca poderosa mas simples para mapeamento de objetos em .NET. Com sua API intuitiva e flexível, ela oferece uma alternativa leve ao AutoMapper, mantendo as funcionalidades essenciais para a maioria dos casos de uso em aplicações .NET.

### Vantagens do Mirror

- ✅ **Curva de aprendizado suave**
- ✅ **Código limpo e manutenível**
- ✅ **Performance otimizada**
- ✅ **Integração nativa com DI**
- ✅ **Sem dependências externas**
- ✅ **Totalmente testado**

### Quando Usar

- Aplicações que precisam de mapeamento simples e direto
- Projetos que valorizam simplicidade sobre complexidade
- Times que preferem controle explícito sobre mapeamentos
- Microserviços e aplicações leves

### Quando Considerar Alternativas

- Necessidade de mapeamentos extremamente complexos
- Requisitos avançados de projeção de queries
- Integração com ORMs específicos

---

**Mirror** - Refletindo seus objetos com simplicidade e elegância! 🪞