using Mirror.Exceptions;
using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class ExtensionTests
{
	[Fact]
	public void Reflect_Extension_Deve_Inferir_Tipo_Origem()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoa = new Pessoa { Id = 1, Nome = "João" };

		// Act
		var dto = pessoa.Reflect<PessoaDto>(mirror);

		// Assert
		Assert.NotNull(dto);
		Assert.Equal(pessoa.Id, dto.Id);
		Assert.Equal(pessoa.Nome, dto.Nome);
	}

	[Fact]
	public void Reflect_Extension_Sem_Parametro_Mirror_Deve_Funcionar()
	{
		MirrorExtensions.ResetDefaultMirror();

		var pessoa = new Pessoa { Id = 3, Nome = "Carlos" };

		var dto = pessoa.Reflect<PessoaDto>();

		Assert.NotNull(dto);
		Assert.Equal(3, dto.Id);
		Assert.Equal("Carlos", dto.Nome);
	}

	[Fact]
	public void ReflectTo_Extension_Deve_Atualizar_Objeto_Existente()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoa = new Pessoa { Id = 1, Nome = "João" };
		var dto = new PessoaDto { Id = 0, Nome = "Original" };

		// Act
		pessoa.ReflectTo(dto, mirror);

		// Assert
		Assert.Equal(1, dto.Id);
		Assert.Equal("João", dto.Nome);
	}

	[Fact]
	public void ReflectAll_Extension_Deve_Mapear_Colecao()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoas = new List<Pessoa>
		{
			new() { Id = 1, Nome = "João" },
			new() { Id = 2, Nome = "Maria" }
		};

		// Act
		var dtos = pessoas.ReflectAll<PessoaDto>(mirror).ToList();

		// Assert
		Assert.Equal(2, dtos.Count);
		Assert.Equal(1, dtos[0].Id);
		Assert.Equal("João", dtos[0].Nome);
		Assert.Equal(2, dtos[1].Id);
		Assert.Equal("Maria", dtos[1].Nome);
	}

	[Fact]
	public void ReflectAll_Extension_Sem_Parametro_Mirror_Deve_Funcionar()
	{
		MirrorExtensions.ResetDefaultMirror();

		var pessoas = new List<Pessoa>
		{
			new() { Id = 1, Nome = "João" },
			new() { Id = 2, Nome = "Maria" }
		};

		var dtos = pessoas.ReflectAll<Pessoa, PessoaDto>().ToList();

		Assert.Equal(2, dtos.Count);
		Assert.Equal("João", dtos[0].Nome);
		Assert.Equal("Maria", dtos[1].Nome);
	}

	[Fact]
	public void ReflectAll_Com_Tipo_Especifico_Deve_Mapear_Colecao()
	{
		// Arrange
		var mirror = new Mirror();
		var pessoas = new List<Pessoa>
		{
			new() { Id = 1, Nome = "João" },
			new() { Id = 2, Nome = "Maria" }
		};

		// Act
		var dtos = pessoas.ReflectAll<Pessoa, PessoaDto>(mirror).ToList();

		// Assert
		Assert.Equal(2, dtos.Count);
		Assert.Equal(1, dtos[0].Id);
		Assert.Equal("João", dtos[0].Nome);
		Assert.Equal(2, dtos[1].Id);
		Assert.Equal("Maria", dtos[1].Nome);
	}

	[Fact]
	public void Diagnostico_Factory_Deve_Lancar_Excecao()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var factoryChamada = false;
		Exception? excecaoLancada = null;

		// Usando a classe com validação, não a factory sem validação
		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequestComValidacao>(dto =>
		{
			factoryChamada = true;
			try
			{
				return EmpresaRequestComValidacao.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj);
			}
			catch (Exception ex)
			{
				excecaoLancada = ex;
				throw;
			}
		});

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "", // Inválido - deve lançar exceção
			Cnpj = "123"
		};

		// Act & Assert
		var excecao = Assert.Throws<ArgumentException>(() =>
		{
			mirror.ReflectUsingFactory<CreateEmpresaRequestDto, EmpresaRequestComValidacao>(dto);
		});

		Assert.True(factoryChamada, "Factory deveria ter sido chamada");
		Assert.NotNull(excecaoLancada);
		Assert.Contains("Razão social", excecao.Message);
	}

	[Fact]
	public void ReflectSafe_Deve_Capturar_Excecoes()
	{
		// Arrange
		var config = new MirrorConfiguration();

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequestComValidacao>(dto =>
			EmpresaRequestComValidacao.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj)
		);

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "", // Inválido
			Cnpj = "123"
		};

		var excecaoCapturada = false;
		Exception? excecaoOriginal = null;

		// Act & Assert
		var mirrorExcecao = Assert.Throws<MirrorException>(() =>
		{
			dto.ReflectSafe<EmpresaRequestComValidacao>(mirror, ex =>
			{
				excecaoCapturada = true;
				excecaoOriginal = ex;
			});
		});

		Assert.True(excecaoCapturada);
		Assert.NotNull(excecaoOriginal);
		Assert.IsType<ArgumentException>(excecaoOriginal);

		// Verifica se a mensagem da exceção original contém "Razão social"
		Assert.Contains("Razão social", excecaoOriginal.Message);

		// Verifica se a mensagem do MirrorException contém a mensagem original
		Assert.Contains(excecaoOriginal.Message, mirrorExcecao.Message);
	}

	[Fact]
	public void SetDefaultMirror_Deve_Permitir_Usar_Configuracao_Sem_Passar_Mirror()
	{
		var config = new MirrorConfiguration();
		config.CreateReflection<Pessoa, PessoaDto>()
			.ForMember(dto => dto.Idade, pessoa => 99);

		MirrorExtensions.SetDefaultMirror(new Mirror(config));

		var pessoa = new Pessoa
		{
			Id = 5,
			Nome = "Config Default",
			DataNascimento = new DateTime(1990, 1, 1)
		};

		var dto = pessoa.Reflect<PessoaDto>();

		Assert.Equal(5, dto.Id);
		Assert.Equal("Config Default", dto.Nome);
		Assert.Equal(99, dto.Idade);

		MirrorExtensions.ResetDefaultMirror();
	}

	// Opcional: Teste para verificar se a factory sem validação funciona (não lança exceção)
	[Fact]
	public void Factory_Sem_Validacao_Nao_Deve_Lancar_Excecao()
	{
		// Arrange
		var config = new MirrorConfiguration();

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequestFactory>(dto =>
			EmpresaRequestFactory.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj)
		);

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "", // Inválido mas a factory não valida
			Cnpj = "123"
		};

		// Act - Não deve lançar exceção
		var resultado = mirror.ReflectUsingFactory<CreateEmpresaRequestDto, EmpresaRequestFactory>(dto);

		// Assert
		Assert.NotNull(resultado);
		Assert.Equal("", resultado.RazaoSocial);
		Assert.Equal("123", resultado.Cnpj);
	}

	[Fact]
	public void Diagnostico_Simples_Factory()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var factoryChamada = false;

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequestFactory>(dto =>
		{
			factoryChamada = true;
			Console.WriteLine($"Factory chamada com: RazaoSocial='{dto.RazaoSocial}', Cnpj='{dto.Cnpj}'");
			return EmpresaRequestFactory.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj);
		});

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "", // Inválido
			Cnpj = "123"
		};

		// Act
		var resultado = mirror.ReflectUsingFactory<CreateEmpresaRequestDto, EmpresaRequestFactory>(dto);

		// Assert
		Assert.True(factoryChamada);
		Assert.NotNull(resultado);
		Assert.Equal("", resultado.RazaoSocial); // Deve ter o valor vazio
		Assert.Equal("123", resultado.Cnpj); // Deve ter o valor inválido
	}

	[Fact]
	public void Diagnostico_Com_Validacao_Real()
	{
		// Arrange
		var config = new MirrorConfiguration();

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequestComValidacao>(dto =>
			EmpresaRequestComValidacao.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj)
		);

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "", // Inválido - deve lançar exceção
			Cnpj = "123"
		};

		// Act & Assert - Agora deve lançar ArgumentException diretamente
		var excecao = Assert.Throws<ArgumentException>(() =>
		{
			mirror.ReflectUsingFactory<CreateEmpresaRequestDto, EmpresaRequestComValidacao>(dto);
		});

		Assert.Contains("Razão social", excecao.Message);
	}

	[Fact]
	public void ReflectWithFactory_Deve_Usar_Factory_Especifico()
	{
		// Arrange
		var mirror = new Mirror();
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		var factoryUsada = false;

		// Act
		var result = dto.ReflectWithFactory(mirror, (CreateEmpresaRequestDto origem) =>
		{
			factoryUsada = true;
			return EmpresaRequestFactory.Create(origem.RazaoSocial, origem.Fantasia, origem.Cnpj);
		});

		// Assert
		Assert.True(factoryUsada);
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
		Assert.Equal(dto.Cnpj, result.Cnpj);
	}

	[Fact]
	public void ReflectWithFactory_Object_Version_Deve_Funcionar()
	{
		// Arrange
		var mirror = new Mirror();
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Cnpj = "12345678000195"
		};

		// Act
		var result = dto.ReflectWithFactory<EmpresaRequestFactory>(mirror, obj =>
		{
			var origem = (CreateEmpresaRequestDto)obj;
			return EmpresaRequestFactory.Create(origem.RazaoSocial, origem.Fantasia, origem.Cnpj);
		});

		// Assert
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
	}
}
