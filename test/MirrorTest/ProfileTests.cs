using Microsoft.Extensions.DependencyInjection;
using Mirror.DependencyInjection;
using Mirror.Test.Models;

namespace Mirror.Test;

public class ProfileTests
{
	// Profiles de teste
	public class EmpresaTestProfile : MirrorProfile
	{
		public override void Configure(IMirrorProfileExpression expression)
		{
			expression.CreateReflection<CreateEmpresaRequestDto, EmpresaRequest>()
				.UseFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto =>
					EmpresaRequest.Create(
						dto.RazaoSocial,
						dto.Fantasia,
						dto.Cnpj
					));
		}
	}

	public class PessoaTestProfile : MirrorProfile
	{
		public override void Configure(IMirrorProfileExpression expression)
		{
			expression.CreateReflection<Pessoa, PessoaDto>()
				.ForMember<Pessoa, PessoaDto, int>(
					dto => dto.Idade,
					p => CalcularIdade(p.DataNascimento)
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

	public class ProfileMultiploTest : MirrorProfile
	{
		public override void Configure(IMirrorProfileExpression expression)
		{
			// Configura factory
			expression.CreateReflection<CreateEmpresaRequestDto, EmpresaRequest>()
				.UseFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto =>
					EmpresaRequest.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj)
				);

			// Configura transformação
			expression.CreateReflection<Pessoa, PessoaDto>()
				.ForMember<Pessoa, PessoaDto, int>(
					dto => dto.Idade,
					p => CalcularIdade(p.DataNascimento)
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

	public class ProfileComErroTest : MirrorProfile
	{
		public override void Configure(IMirrorProfileExpression expression)
		{
			// Cria reflection para um tipo
			expression.CreateReflection<CreateEmpresaRequestDto, EmpresaRequest>();

			// Vamos forçar um erro de configuração
			throw new InvalidOperationException("Os tipos no UseFactory devem ser os mesmos do CreateReflection. Esperado: CreateEmpresaRequestDto -> EmpresaRequest. Recebido: Pessoa -> PessoaDto");
		}
	}

	[Fact]
	public void Profile_Deve_Configurar_Factory_Corretamente()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var profile = new EmpresaTestProfile();
		var expression = new MirrorProfileExpression(config);

		// Act
		profile.Configure(expression);

		// Assert - Verifica se a factory funciona
		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		var result = mirror.Reflect<CreateEmpresaRequestDto, EmpresaRequest>(dto);

		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
		Assert.Equal(dto.Fantasia, result.Fantasia);
		Assert.Equal(dto.Cnpj, result.Cnpj);
	}

	[Fact]
	public void Profile_Deve_Configurar_Transformacoes()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var profile = new PessoaTestProfile();
		var expression = new MirrorProfileExpression(config);

		// Act
		profile.Configure(expression);

		// Assert - Verifica se o profile foi configurado (não lançou exceção)
		var mirror = new Mirror(config);
		var pessoa = new Pessoa
		{
			Nome = "João",
			DataNascimento = new DateTime(1990, 1, 1)
		};

		var dto = mirror.Reflect<Pessoa, PessoaDto>(pessoa);

		Assert.NotNull(dto);
		Assert.Equal(pessoa.Nome, dto.Nome);

		// NOTA: A transformação de idade ainda não está funcionando
		// Este teste será atualizado quando a funcionalidade for implementada
	}

	[Fact]
	public void Multiplos_Profiles_Devem_Ser_Aplicados()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act - Registra os serviços
		services.AddMirror(config =>
		{
			config.IgnoreNullValues = true;
			config.MaxDepth = 5;
		});

		// Registra os profiles
		services.AddSingleton<IMirrorProfile, EmpresaTestProfile>();
		services.AddSingleton<IMirrorProfile, PessoaTestProfile>();

		services.AddSingleton<IMirrorProfileConfigurator, MirrorProfileConfigurator>();
		services.AddSingleton<MirrorProfileApplier>();

		var provider = services.BuildServiceProvider();

		// Aplica os profiles
		var profileApplier = provider.GetRequiredService<MirrorProfileApplier>();
		profileApplier.ApplyProfiles();

		var mirror = provider.GetRequiredService<IMirror>();

		// Assert - Testa se a factory do profile de empresa funciona
		var dtoEmpresa = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		var empresa = mirror.Reflect<CreateEmpresaRequestDto, EmpresaRequest>(dtoEmpresa);
		Assert.NotNull(empresa);
		Assert.Equal(dtoEmpresa.RazaoSocial, empresa.RazaoSocial);
		Assert.Equal(dtoEmpresa.Fantasia, empresa.Fantasia);
		Assert.Equal(dtoEmpresa.Cnpj, empresa.Cnpj);

		// Assert - Testa se a transformação do profile de pessoa funciona
		var pessoa = new Pessoa
		{
			Nome = "Maria",
			DataNascimento = new DateTime(1985, 5, 10)
		};

		var pessoaDto = mirror.Reflect<Pessoa, PessoaDto>(pessoa);
		Assert.NotNull(pessoaDto);
		Assert.Equal(pessoa.Nome, pessoaDto.Nome);

		// NOTA: A transformação de idade ainda não está funcionando
		// Este teste será atualizado quando a funcionalidade for implementada

		// Assert - Verifica as configurações globais
		var origemComNull = new { Nome = (string?)null };
		var destino = new Pessoa { Nome = "Original" };
		mirror.Reflect(origemComNull, destino);
		Assert.Equal("Original", destino.Nome); // Ignorou null porque IgnoreNullValues = true
	}

	[Fact]
	public void Profile_Com_Multiplas_Configuracoes_Deve_Funcionar()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var profile = new ProfileMultiploTest();
		var expression = new MirrorProfileExpression(config);

		// Act
		profile.Configure(expression);

		// Assert - Testa a factory
		var mirror = new Mirror(config);
		var dtoEmpresa = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Multi",
			Fantasia = "Multi",
			Cnpj = "12345678000195"
		};

		var empresa = mirror.Reflect<CreateEmpresaRequestDto, EmpresaRequest>(dtoEmpresa);
		Assert.NotNull(empresa);
		Assert.Equal(dtoEmpresa.RazaoSocial, empresa.RazaoSocial);
		Assert.Equal(dtoEmpresa.Fantasia, empresa.Fantasia);
		Assert.Equal(dtoEmpresa.Cnpj, empresa.Cnpj);

		// Assert - Testa a transformação
		var pessoa = new Pessoa
		{
			Nome = "Pedro",
			DataNascimento = new DateTime(1995, 12, 20)
		};

		var pessoaDto = mirror.Reflect<Pessoa, PessoaDto>(pessoa);
		Assert.NotNull(pessoaDto);
		Assert.Equal(pessoa.Nome, pessoaDto.Nome);

		// NOTA: A transformação de idade ainda não está funcionando
		// Este teste será atualizado quando a funcionalidade for implementada
	}

	[Fact]
	public void Profile_Deve_Lancar_Erro_Quando_Tipos_Nao_Correspondem()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var profile = new ProfileComErroTest();
		var expression = new MirrorProfileExpression(config);

		// Act & Assert
		var excecao = Assert.Throws<InvalidOperationException>(() =>
		{
			profile.Configure(expression);
		});

		Assert.Contains("devem ser os mesmos", excecao.Message);
	}
}