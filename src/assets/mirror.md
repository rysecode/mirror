# Mirror

Mirror é uma biblioteca de mapeamento objeto-objeto para .NET, criada para oferecer uma experiência simples, explícita e flexível na conversão entre modelos de domínio, DTOs, requests e responses.

## Objetivo

O objetivo do Mirror é reduzir código repetitivo de cópia entre objetos sem sacrificar clareza. A biblioteca foi desenhada para cenários comuns de aplicações .NET onde precisamos:

- mapear propriedades de mesmo nome entre dois tipos
- atualizar um objeto de destino já existente
- tratar objetos aninhados, listas e dicionários
- aplicar transformações em propriedades específicas
- usar factories para tipos com regras de criação próprias
- integrar o mapeamento com injeção de dependência

O Mirror prioriza uma API pequena e previsível. Em vez de esconder o comportamento atrás de muitas convenções implícitas, ele busca entregar um fluxo fácil de entender, testar e manter.

## Implementação

O Mirror funciona com base em reflexão sobre propriedades públicas de instância.

Durante o mapeamento, a biblioteca:

1. localiza propriedades compatíveis entre origem e destino por nome
2. aplica transformações configuradas, quando existirem
3. respeita propriedades ignoradas por atributo ou por expressão
4. copia valores simples diretamente
5. materializa coleções e dicionários quando necessário
6. mapeia recursivamente objetos complexos
7. usa factories registradas quando um tipo precisa de criação customizada

Recursos suportados atualmente:

- mapeamento entre objetos simples
- mapeamento em novo objeto ou em instância já existente
- listas, arrays e várias coleções genéricas
- `Dictionary`, `SortedList`, `ReadOnlyDictionary`, `ConcurrentDictionary`, `ImmutableDictionary`, `Hashtable` e `IDictionary`
- objetos complexos profundamente aninhados
- `IgnoreNullValues`
- `MaxDepth`
- profiles
- methods de extensão
- ignore de propriedades por atributo e por expressão

## Instalação

### .NET CLI

```bash
dotnet add package Mirror
```

### PackageReference

```xml
<PackageReference Include="Mirror" Version="1.0.1" />
```

## Uso Básico

### Criando um novo objeto de destino

```csharp
using Mirror;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

var mirror = new Mirror.Mirror();

var user = new User
{
    Id = 1,
    Name = "Ana",
    Email = "ana@company.com"
};

var dto = mirror.Reflect<User, UserDto>(user);
```

### Atualizando um objeto já existente

```csharp
var source = new User
{
    Id = 1,
    Name = "Ana",
    Email = "ana@company.com"
};

var destination = new UserDto
{
    Id = 99,
    Name = "Original",
    Email = "original@company.com"
};

mirror.Reflect(source, destination);
```

## Ignorando Propriedades

O Mirror permite ignorar propriedades de duas formas.

### 1. Por atributo

```csharp
using Mirror;

public class HouseDto
{
    [MirrorNonReflect]
    public string Name { get; set; } = string.Empty;

    public int Doors { get; set; }
    public int Windows { get; set; }
}
```

Quando essa propriedade estiver no destino, o valor atual dela será preservado durante o `Reflect`.

### 2. Por expressão na chamada

```csharp
var source = new House
{
    Name = "Lar",
    Doors = 2,
    Windows = 5
};

var destination = new HouseDto
{
    Name = "Apartamento",
    Doors = 3,
    Windows = 4
};

mirror.Reflect(source, destination, x => x.Name);
mirror.Reflect(source, destination, x => x.Name, x => x.Windows);
```

Também é possível ignorar propriedades ao criar um novo objeto:

```csharp
var dto = mirror.Reflect<House, HouseDto>(source, x => x.Name);
```

## Transformações Customizadas

Transformações permitem montar propriedades de destino com regras específicas.

```csharp
var configuration = new MirrorConfiguration();

configuration.CreateReflection<User, UserDto>()
    .ForMember(dto => dto.Name, user => user.Name.ToUpperInvariant());

var mirror = new Mirror.Mirror(configuration);
var dto = mirror.Reflect<User, UserDto>(user);
```

## Factory Methods

Factories são úteis quando o destino não deve ser criado apenas com `new()`, ou quando existe uma regra de construção de domínio.

```csharp
var configuration = new MirrorConfiguration();

configuration.AddFactory<CreateCompanyRequest, Company>(request =>
    Company.Create(request.Name, request.Document)
);

var mirror = new Mirror.Mirror(configuration);

var company = mirror.Reflect<CreateCompanyRequest, Company>(request);
```

Também é possível usar uma factory diretamente na chamada:

```csharp
var result = mirror.ReflectWithFactory(request, source =>
    Company.Create(source.Name, source.Document)
);
```

## Profiles

Profiles ajudam a centralizar e organizar regras de mapeamento.

```csharp
using Mirror;

public class UserProfile : MirrorProfile
{
    public override void Configure(IMirrorProfileExpression expression)
    {
        expression.CreateReflection<User, UserDto>()
            .ForMember<User, UserDto, string>(
                dto => dto.Name,
                user => $"{user.Name} - ACTIVE"
            );
    }
}
```

## Métodos de Extensão

Além do uso direto com `IMirror`, o Mirror oferece extensões para uma sintaxe mais fluida.

### Novo objeto com instância padrão

```csharp
using Mirror.Extensions;

var dto = user.Reflect<UserDto>();
```

### Atualizando instância existente

```csharp
user.ReflectTo(existingDto);
```

### Ignorando membros pela extensão

```csharp
user.ReflectTo(existingDto, x => x.Name);
```

### Mapeando coleções

```csharp
var users = new List<User>
{
    new() { Id = 1, Name = "Ana" },
    new() { Id = 2, Name = "Bruno" }
};

var dtos = users.ReflectAll<User, UserDto>().ToList();
```

### Configurando um Mirror padrão para as extensões

```csharp
using Mirror.Extensions;

var configuration = new MirrorConfiguration();

configuration.CreateReflection<User, UserDto>()
    .ForMember(dto => dto.Name, user => user.Name.ToUpperInvariant());

MirrorExtensions.SetDefaultMirror(new Mirror.Mirror(configuration));

var dto = user.Reflect<UserDto>();

MirrorExtensions.ResetDefaultMirror();
```

## Coleções e Dicionários

O Mirror suporta o mapeamento de coleções simples e profundas, incluindo objetos complexos em múltiplos níveis.

Exemplos de coleções suportadas:

- `List<T>`
- `ICollection<T>`
- `IReadOnlyList<T>`
- `Collection<T>`
- `ObservableCollection<T>`
- `ReadOnlyCollection<T>`
- `ImmutableList<T>`
- `ConcurrentBag<T>`
- `LinkedList<T>`
- `Queue<T>`
- `Stack<T>`
- arrays

Exemplos de mapas suportados:

- `Dictionary<TKey, TValue>`
- `SortedList<TKey, TValue>`
- `IReadOnlyDictionary<TKey, TValue>`
- `ReadOnlyDictionary<TKey, TValue>`
- `ConcurrentDictionary<TKey, TValue>`
- `ImmutableDictionary<TKey, TValue>`
- `Hashtable`
- `IDictionary`

## Injeção de Dependência

O pacote possui integração com `Microsoft.Extensions.DependencyInjection`.

### Registro básico

```csharp
using Mirror.DependencyInjection;

builder.Services.AddMirror(config =>
{
    config.IgnoreNullValues = true;
    config.MaxDepth = 10;
});
```

### Registro com profiles

```csharp
using Mirror.DependencyInjection;
using System.Reflection;

builder.Services.AddMirrorWithProfiles(
    configure: config =>
    {
        config.IgnoreNullValues = true;
        config.MaxDepth = 10;
    },
    Assembly.GetExecutingAssembly()
);
```

Depois disso, basta injetar `IMirror`:

```csharp
public class UserService
{
    private readonly IMirror _mirror;

    public UserService(IMirror mirror)
    {
        _mirror = mirror;
    }

    public UserDto Map(User user)
    {
        return _mirror.Reflect<User, UserDto>(user);
    }
}
```

## Comportamentos Importantes

### IgnoreNullValues

Quando `IgnoreNullValues` está habilitado, valores `null` da origem não sobrescrevem o destino.

```csharp
var config = new MirrorConfiguration
{
    IgnoreNullValues = true
};
```

### MaxDepth

Define a profundidade máxima de mapeamento para grafos complexos e ajuda a evitar recursão excessiva.

```csharp
var config = new MirrorConfiguration
{
    MaxDepth = 5
};
```

## Tratamento de Exceções

Erros de mapeamento são encapsulados em `MirrorException`, preservando a exceção interna original.

```csharp
try
{
    var result = source.ReflectSafe<Destination>();
}
catch (MirrorException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.InnerException?.Message);
}
```

## Quando Usar

O Mirror é indicado para:

- aplicações que precisam de mapeamento claro e direto
- APIs que convertem entidades em DTOs com frequência
- projetos que preferem controle explícito sobre o pipeline de mapeamento
- cenários em que listas, dicionários e grafos complexos fazem parte do fluxo

## Resumo

Mirror entrega uma base sólida para mapeamento objeto-objeto em .NET com foco em simplicidade, previsibilidade e extensibilidade. A biblioteca já suporta cenários básicos, profundos e híbridos com listas, dicionários, profiles, factories e regras de ignore, mantendo uma API pequena e fácil de adotar.
