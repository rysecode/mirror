using Mirror.Exceptions;
using System.Collections.Concurrent;
using System.Reflection;

namespace Mirror.Extensions;

public static class MirrorExtensions
{
	private static readonly ConcurrentDictionary<(Type, Type), Func<object, IMirror, object>> _reflectCache = new();

	/// <summary>
	/// Reflete o objeto de origem para um novo objeto do tipo destino.
	/// O tipo de origem é inferido automaticamente a partir do objeto.
	/// </summary>
	/// <typeparam name="TDestino">O tipo do objeto destino</typeparam>
	/// <param name="origem">O objeto de origem</param>
	/// <param name="mirror">A instância do Mirror</param>
	/// <returns>Uma nova instância do tipo destino com os valores refletidos</returns>
	public static TDestino Reflect<TDestino>(this object origem, IMirror mirror) where TDestino : new()
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));

		if (mirror == null)
			throw new ArgumentNullException(nameof(mirror));

		var origemType = origem.GetType();
		var destinoType = typeof(TDestino);

		var method = typeof(IMirror).GetMethods()
			.FirstOrDefault(m => m.Name == nameof(IMirror.Reflect) &&
								m.IsGenericMethod &&
								m.GetGenericArguments().Length == 2 &&
								m.GetParameters().Length == 1 &&
								m.ReturnType.IsGenericParameter);

		if (method == null)
			throw new InvalidOperationException($"Não foi possível encontrar o método Reflect para {origemType.Name} -> {destinoType.Name}");

		var genericMethod = method.MakeGenericMethod(origemType, destinoType);

		return (TDestino)genericMethod.Invoke(mirror, new[] { origem })!;
	}

	/// <summary>
	/// Reflete o objeto de origem para um objeto destino existente.
	/// </summary>
	public static void ReflectTo<TOrigem, TDestino>(this TOrigem origem, TDestino destino, IMirror mirror)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));

		if (destino == null)
			throw new ArgumentNullException(nameof(destino));

		if (mirror == null)
			throw new ArgumentNullException(nameof(mirror));

		mirror.Reflect(origem, destino);
	}

	/// <summary>
	/// Reflete uma coleção de objetos de origem para uma nova coleção de objetos destino.
	/// </summary>
	public static IEnumerable<TDestino> ReflectAll<TDestino>(
		this IEnumerable<object> origens,
		IMirror mirror) where TDestino : new()
	{
		if (origens == null)
			return Enumerable.Empty<TDestino>();

		return origens.Select(origem => origem.Reflect<TDestino>(mirror));
	}

	/// <summary>
	/// Versão genérica para coleções com tipo de origem conhecido.
	/// </summary>
	public static IEnumerable<TDestino> ReflectAll<TOrigem, TDestino>(
		this IEnumerable<TOrigem> origens,
		IMirror mirror) where TDestino : new()
	{
		if (origens == null)
			return Enumerable.Empty<TDestino>();

		return origens.Select(origem => mirror.Reflect<TOrigem, TDestino>(origem));
	}

	/// <summary>
	/// Versão segura que captura exceções durante o mapeamento.
	/// </summary>
	public static TDestino ReflectSafe<TDestino>(
		this object origem,
		IMirror mirror,
		Action<Exception>? onError = null)
	{
		try
		{
			if (origem == null)
				throw new ArgumentNullException(nameof(origem));
			if (mirror == null)
				throw new ArgumentNullException(nameof(mirror));

			var origemType = origem.GetType();
			var destinoType = typeof(TDestino);

			// Tenta primeiro com factory (sem constraint)
			try
			{
				return origem.ReflectUsingFactory<TDestino>(mirror);
			}
			catch (InvalidOperationException) // Se não tem factory, tenta com new()
			{
				// Usa o método Reflect normal com constraint
				var method = typeof(IMirror).GetMethods()
					.FirstOrDefault(m => m.Name == nameof(IMirror.Reflect) &&
										m.IsGenericMethod &&
										m.GetGenericArguments().Length == 2 &&
										m.GetParameters().Length == 1);

				if (method == null)
					throw new InvalidOperationException("Método Reflect não encontrado");

				var genericMethod = method.MakeGenericMethod(origemType, destinoType);

				return (TDestino)genericMethod.Invoke(mirror, new[] { origem })!;
			}
		}
		catch (TargetInvocationException ex)
		{
			var innerEx = ex.InnerException ?? ex;
			onError?.Invoke(innerEx);
			// Inclui a mensagem da exceção original no MirrorException
			throw new MirrorException($"Erro ao refletir {origem?.GetType().Name} para {typeof(TDestino).Name}: {innerEx.Message}", innerEx);
		}
		catch (Exception ex)
		{
			onError?.Invoke(ex);
			throw new MirrorException($"Erro ao refletir {origem?.GetType().Name} para {typeof(TDestino).Name}: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Versão específica para tipos com factory (mantém constraint para compatibilidade)
	/// </summary>
	public static TDestino ReflectSafeWithNew<TDestino>(
		this object origem,
		IMirror mirror,
		Action<Exception>? onError = null) where TDestino : new()
	{
		try
		{
			return origem.Reflect<TDestino>(mirror);
		}
		catch (Exception ex)
		{
			onError?.Invoke(ex);
			throw new MirrorException($"Erro ao refletir {origem?.GetType().Name} para {typeof(TDestino).Name}", ex);
		}
	}

	/// <summary>
	/// Reflete usando um factory method específico, com sintaxe fluente.
	/// </summary>
	public static TDestino ReflectWithFactory<TDestino>(
		this object origem,
		IMirror mirror,
		Func<object, TDestino> factory)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));
		if (mirror == null)
			throw new ArgumentNullException(nameof(mirror));
		if (factory == null)
			throw new ArgumentNullException(nameof(factory));

		var origemType = origem.GetType();
		var destinoType = typeof(TDestino);

		var method = typeof(IMirror).GetMethods()
			.First(m => m.Name == nameof(IMirror.ReflectWithFactory) &&
					   m.IsGenericMethod &&
					   m.GetParameters().Length == 2);

		var genericMethod = method.MakeGenericMethod(origemType, destinoType);

		// Cria um factory que aceita object e converte
		Func<object, TDestino> objectFactory = obj => factory(obj);

		return (TDestino)genericMethod.Invoke(mirror, new[] { origem, objectFactory })!;
	}

	/// <summary>
	/// Versão tipada do ReflectWithFactory.
	/// </summary>
	public static TDestino ReflectWithFactory<TOrigem, TDestino>(
		this TOrigem origem,
		IMirror mirror,
		Func<TOrigem, TDestino> factory)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));
		if (mirror == null)
			throw new ArgumentNullException(nameof(mirror));
		if (factory == null)
			throw new ArgumentNullException(nameof(factory));

		return mirror.ReflectWithFactory(origem, factory);
	}

	public static TDestino ReflectUsingFactory<TDestino>(this object origem, IMirror mirror)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));
		if (mirror == null)
			throw new ArgumentNullException(nameof(mirror));

		var origemType = origem.GetType();
		var destinoType = typeof(TDestino);

		var method = typeof(IMirror).GetMethods()
			.FirstOrDefault(m => m.Name == nameof(IMirror.ReflectUsingFactory) &&
								m.IsGenericMethod &&
								m.GetGenericArguments().Length == 2 &&
								m.GetParameters().Length == 1);

		if (method == null)
			throw new InvalidOperationException("Método ReflectUsingFactory não encontrado");

		var genericMethod = method.MakeGenericMethod(origemType, destinoType);

		try
		{
			return (TDestino)genericMethod.Invoke(mirror, new[] { origem })!;
		}
		catch (TargetInvocationException ex)
		{
			throw ex.InnerException ?? ex;
		}
	}
}