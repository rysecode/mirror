using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Mirror.DependencyInjection;

public static class MirrorServiceCollectionExtensions
{
	public static IServiceCollection AddMirror(this IServiceCollection services, Action<MirrorConfiguration>? configure = null)
	{
		var configuration = new MirrorConfiguration();
		configure?.Invoke(configuration);

		// Registra a configuração como singleton
		services.AddSingleton(configuration);

		// Registra o Mirror como scoped (pode ser singleton também)
		services.AddScoped<IMirror>(provider =>
		{
			var config = provider.GetRequiredService<MirrorConfiguration>();
			return new Mirror(config);
		});

		return services;
	}

	public static IServiceCollection AddMirrorProfiles(this IServiceCollection services, params Assembly[] assemblies)
	{
		if (assemblies == null || assemblies.Length == 0)
			assemblies = new[] { Assembly.GetCallingAssembly() };

		var profileType = typeof(IMirrorProfile);
		var profiles = new List<Type>();

		foreach (var assembly in assemblies)
		{
			profiles.AddRange(assembly.GetTypes()
				.Where(t => profileType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract));
		}

		foreach (var profile in profiles)
		{
			services.AddTransient(typeof(IMirrorProfile), profile);
		}

		// Registra o serviço que irá configurar todos os profiles
		services.AddSingleton<IMirrorProfileConfigurator, MirrorProfileConfigurator>();

		return services;
	}

	public static IServiceCollection AddMirrorWithProfiles(this IServiceCollection services, Action<MirrorConfiguration>? configure = null, params Assembly[] assemblies)
	{
		services.AddMirror(configure);
		services.AddMirrorProfiles(assemblies);

		services.AddSingleton<MirrorProfileApplier>();
		services.AddScoped<IMirror>(provider =>
		{
			provider.GetRequiredService<MirrorProfileApplier>().ApplyProfiles();
			var config = provider.GetRequiredService<MirrorConfiguration>();
			return new Mirror(config);
		});

		return services;
	}
}

public interface IMirrorProfileConfigurator
{
	void ApplyProfiles(MirrorConfiguration configuration, IEnumerable<IMirrorProfile> profiles);
}

public class MirrorProfileConfigurator : IMirrorProfileConfigurator
{
	public void ApplyProfiles(MirrorConfiguration configuration, IEnumerable<IMirrorProfile> profiles)
	{
		var expression = new MirrorProfileExpression(configuration);

		foreach (var profile in profiles)
		{
			profile.Configure(expression);
		}
	}
}

// Hosted service para aplicar profiles na inicialização
public class MirrorProfileApplier
{
	private readonly MirrorConfiguration _configuration;
	private readonly IEnumerable<IMirrorProfile> _profiles;
	private readonly IMirrorProfileConfigurator _configurator;
	private readonly object _sync = new();
	private bool _applied;

	public MirrorProfileApplier(
		MirrorConfiguration configuration,
		IEnumerable<IMirrorProfile> profiles,
		IMirrorProfileConfigurator configurator)
	{
		_configuration = configuration;
		_profiles = profiles;
		_configurator = configurator;
	}

	public void ApplyProfiles()
	{
		if (_applied)
			return;

		lock (_sync)
		{
			if (_applied)
				return;

			_configurator.ApplyProfiles(_configuration, _profiles);
			_applied = true;
		}
	}
}
