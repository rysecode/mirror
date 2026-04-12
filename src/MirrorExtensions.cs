using Mirror.Exceptions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Mirror.Extensions;

public static class MirrorExtensions
{
	private static readonly ConcurrentDictionary<(Type, Type), Func<object, IMirror, object>> _reflectCache = new();
	private static IMirror _defaultMirror = new global::Mirror.Mirror();

	public static void SetDefaultMirror(IMirror mirror)
	{
		_defaultMirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
	}

	public static void ResetDefaultMirror()
	{
		_defaultMirror = new global::Mirror.Mirror();
	}

	private static IMirror GetMirrorOrDefault(IMirror? mirror = null)
	{
		return mirror ?? _defaultMirror;
	}

	public static TDestino Reflect<TDestino>(this object origem) where TDestino : new()
	{
		return origem.Reflect<TDestino>(GetMirrorOrDefault());
	}

	public static TDestino Reflect<TDestino>(this object origem, params Expression<Func<TDestino, object?>>[] ignoreMembers) where TDestino : new()
	{
		return origem.Reflect(GetMirrorOrDefault(), ignoreMembers);
	}

	public static TDestino Reflect<TDestino>(this object origem, IMirror mirror) where TDestino : new()
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));

		mirror = GetMirrorOrDefault(mirror);

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

	public static TDestino Reflect<TDestino>(
		this object origem,
		IMirror mirror,
		params Expression<Func<TDestino, object?>>[] ignoreMembers) where TDestino : new()
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));

		mirror = GetMirrorOrDefault(mirror);

		var origemType = origem.GetType();
		var destinoType = typeof(TDestino);

		var method = typeof(IMirror).GetMethods()
			.FirstOrDefault(m => m.Name == nameof(IMirror.Reflect) &&
								m.IsGenericMethod &&
								m.GetGenericArguments().Length == 2 &&
								m.GetParameters().Length == 2 &&
								m.ReturnType.IsGenericParameter);

		if (method == null)
			throw new InvalidOperationException($"Não foi possível encontrar o método Reflect com ignoreMembers para {origemType.Name} -> {destinoType.Name}");

		var genericMethod = method.MakeGenericMethod(origemType, destinoType);

		return (TDestino)genericMethod.Invoke(mirror, new object[] { origem, ignoreMembers })!;
	}

	public static void ReflectTo<TOrigem, TDestino>(this TOrigem origem, TDestino destino)
	{
		origem.ReflectTo(destino, GetMirrorOrDefault());
	}

	public static void ReflectTo<TOrigem, TDestino>(
		this TOrigem origem,
		TDestino destino,
		params Expression<Func<TDestino, object?>>[] ignoreMembers)
	{
		origem.ReflectTo(destino, GetMirrorOrDefault(), ignoreMembers);
	}

	public static void ReflectTo<TOrigem, TDestino>(this TOrigem origem, TDestino destino, IMirror mirror)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));

		if (destino == null)
			throw new ArgumentNullException(nameof(destino));

		mirror = GetMirrorOrDefault(mirror);

		mirror.Reflect(origem, destino);
	}

	public static void ReflectTo<TOrigem, TDestino>(
		this TOrigem origem,
		TDestino destino,
		IMirror mirror,
		params Expression<Func<TDestino, object?>>[] ignoreMembers)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));

		if (destino == null)
			throw new ArgumentNullException(nameof(destino));

		mirror = GetMirrorOrDefault(mirror);

		mirror.Reflect(origem, destino, ignoreMembers);
	}

	public static IEnumerable<TDestino> ReflectAll<TDestino>(
		this IEnumerable<object> origens) where TDestino : new()
	{
		return origens.ReflectAll<TDestino>(GetMirrorOrDefault());
	}

	public static IEnumerable<TDestino> ReflectAll<TDestino>(
		this IEnumerable<object> origens,
		IMirror mirror) where TDestino : new()
	{
		if (origens == null)
			return Enumerable.Empty<TDestino>();

		mirror = GetMirrorOrDefault(mirror);

		return origens.Select(origem => origem.Reflect<TDestino>(mirror));
	}

	public static IEnumerable<TDestino> ReflectAll<TOrigem, TDestino>(
		this IEnumerable<TOrigem> origens) where TDestino : new()
	{
		return origens.ReflectAll<TOrigem, TDestino>(GetMirrorOrDefault());
	}

	public static IEnumerable<TDestino> ReflectAll<TOrigem, TDestino>(
		this IEnumerable<TOrigem> origens,
		IMirror mirror) where TDestino : new()
	{
		if (origens == null)
			return Enumerable.Empty<TDestino>();

		mirror = GetMirrorOrDefault(mirror);

		return origens.Select(origem => mirror.Reflect<TOrigem, TDestino>(origem));
	}

	public static TDestino ReflectSafe<TDestino>(
		this object origem,
		Action<Exception>? onError = null)
	{
		return origem.ReflectSafe<TDestino>(GetMirrorOrDefault(), onError);
	}

	public static TDestino ReflectSafe<TDestino>(
		this object origem,
		IMirror mirror,
		Action<Exception>? onError = null)
	{
		try
		{
			if (origem == null)
				throw new ArgumentNullException(nameof(origem));
			mirror = GetMirrorOrDefault(mirror);

			var origemType = origem.GetType();
			var destinoType = typeof(TDestino);

			try
			{
				return origem.ReflectUsingFactory<TDestino>(mirror);
			}
			catch (InvalidOperationException)
			{
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
			throw new MirrorException($"Erro ao refletir {origem?.GetType().Name} para {typeof(TDestino).Name}: {innerEx.Message}", innerEx);
		}
		catch (Exception ex)
		{
			onError?.Invoke(ex);
			throw new MirrorException($"Erro ao refletir {origem?.GetType().Name} para {typeof(TDestino).Name}: {ex.Message}", ex);
		}
	}

	public static TDestino ReflectSafeWithNew<TDestino>(
		this object origem,
		Action<Exception>? onError = null) where TDestino : new()
	{
		return origem.ReflectSafeWithNew<TDestino>(GetMirrorOrDefault(), onError);
	}

	public static TDestino ReflectSafeWithNew<TDestino>(
		this object origem,
		IMirror mirror,
		Action<Exception>? onError = null) where TDestino : new()
	{
		try
		{
			mirror = GetMirrorOrDefault(mirror);
			return origem.Reflect<TDestino>(mirror);
		}
		catch (Exception ex)
		{
			onError?.Invoke(ex);
			throw new MirrorException($"Erro ao refletir {origem?.GetType().Name} para {typeof(TDestino).Name}", ex);
		}
	}

	public static TDestino ReflectWithFactory<TDestino>(
		this object origem,
		Func<object, TDestino> factory)
	{
		return origem.ReflectWithFactory(GetMirrorOrDefault(), factory);
	}

	public static TDestino ReflectWithFactory<TDestino>(
		this object origem,
		IMirror mirror,
		Func<object, TDestino> factory)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));
		mirror = GetMirrorOrDefault(mirror);
		if (factory == null)
			throw new ArgumentNullException(nameof(factory));

		var origemType = origem.GetType();
		var destinoType = typeof(TDestino);

		var method = typeof(IMirror).GetMethods()
			.First(m => m.Name == nameof(IMirror.ReflectWithFactory) &&
					   m.IsGenericMethod &&
					   m.GetParameters().Length == 2);

		var genericMethod = method.MakeGenericMethod(origemType, destinoType);

		Func<object, TDestino> objectFactory = obj => factory(obj);

		return (TDestino)genericMethod.Invoke(mirror, new object[] { origem, objectFactory })!;
	}

	public static TDestino ReflectWithFactory<TOrigem, TDestino>(
		this TOrigem origem,
		Func<TOrigem, TDestino> factory)
	{
		return origem.ReflectWithFactory(GetMirrorOrDefault(), factory);
	}

	public static TDestino ReflectWithFactory<TOrigem, TDestino>(
		this TOrigem origem,
		IMirror mirror,
		Func<TOrigem, TDestino> factory)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));
		mirror = GetMirrorOrDefault(mirror);
		if (factory == null)
			throw new ArgumentNullException(nameof(factory));

		return mirror.ReflectWithFactory(origem, factory);
	}

	public static TDestino ReflectUsingFactory<TDestino>(this object origem)
	{
		return origem.ReflectUsingFactory<TDestino>(GetMirrorOrDefault());
	}

	public static TDestino ReflectUsingFactory<TDestino>(this object origem, IMirror mirror)
	{
		if (origem == null)
			throw new ArgumentNullException(nameof(origem));
		mirror = GetMirrorOrDefault(mirror);

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
