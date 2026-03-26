using Mirror.Extensions;
using Mirror.Test.Models;

namespace Mirror.Test;

public class FactoryTests
{
	[Fact]
	public void Deve_Usar_Factory_Registrada_Na_Configuracao()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var factoryChamada = false;

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto =>
		{
			factoryChamada = true;
			return EmpresaRequest.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj);
		});

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		// Act
		var result = mirror.Reflect<CreateEmpresaRequestDto, EmpresaRequest>(dto);

		// Assert
		Assert.True(factoryChamada, "Factory deveria ter sido chamada");
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
		Assert.Equal(dto.Fantasia, result.Fantasia);
		Assert.Equal(dto.Cnpj, result.Cnpj);
	}

	[Fact]
	public void Deve_Usar_Factory_Do_Profile()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var profile = new TestProfile();
		profile.Configure(new MirrorProfileExpression(config));

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		// Act - Usando o método de extensão Reflect
		var result = dto.Reflect<EmpresaRequest>(mirror);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
		Assert.Equal(dto.Fantasia, result.Fantasia);
		Assert.Equal(dto.Cnpj, result.Cnpj);
	}

	[Fact]
	public void Deve_Usar_ReflectWithFactory_Ignorando_Registradas()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var factoryRegistradaChamada = false;

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto =>
		{
			factoryRegistradaChamada = true;
			return EmpresaRequest.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj);
		});

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		var factoryEspecificaChamada = false;

		// Act
		var result = mirror.ReflectWithFactory(dto, (CreateEmpresaRequestDto origem) =>
		{
			factoryEspecificaChamada = true;
			return EmpresaRequest.Create(origem.RazaoSocial, origem.Fantasia, origem.Cnpj);
		});

		// Assert
		Assert.True(factoryEspecificaChamada, "Factory específica deveria ter sido chamada");
		Assert.False(factoryRegistradaChamada, "Factory registrada NÃO deveria ter sido chamada");
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
		Assert.Equal(dto.Cnpj, result.Cnpj);
	}

	[Fact]
	public void Deve_Usar_Factory_Com_ReflectUsingFactory()
	{
		// Arrange
		var config = new MirrorConfiguration();
		var factoryChamada = false;

		config.AddFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto =>
		{
			factoryChamada = true;
			return EmpresaRequest.Create(dto.RazaoSocial, dto.Fantasia, dto.Cnpj);
		});

		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		// Act
		var result = mirror.ReflectUsingFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto);

		// Assert
		Assert.True(factoryChamada);
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
	}

	[Fact]
	public void Quando_Sem_Factory_Deve_Usar_New()
	{
		// Arrange
		var config = new MirrorConfiguration(); // Sem factory registrada
		var mirror = new Mirror(config);
		var dto = new CreateEmpresaRequestDto
		{
			RazaoSocial = "Empresa Teste",
			Fantasia = "Teste",
			Cnpj = "12345678000195"
		};

		// Act
		var result = mirror.Reflect<CreateEmpresaRequestDto, EmpresaRequest>(dto);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(dto.RazaoSocial, result.RazaoSocial);
		Assert.Equal(dto.Fantasia, result.Fantasia);
		Assert.Equal(dto.Cnpj, result.Cnpj);
	}

	public class TestProfile : MirrorProfile
	{
		public override void Configure(IMirrorProfileExpression expression)
		{
			// Especificando os tipos explicitamente no UseFactory
			expression.CreateReflection<CreateEmpresaRequestDto, EmpresaRequest>()
				.UseFactory<CreateEmpresaRequestDto, EmpresaRequest>(dto =>
					EmpresaRequest.Create(
						razaoSocial: dto.RazaoSocial,
						fantasia: dto.Fantasia,
						cnpj: dto.Cnpj
					)
				);
		}
	}
}