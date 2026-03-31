using Mirror.Exceptions;
using System.Collections;
using System.Reflection;

namespace Mirror;

public class Mirror : IMirror
{
	private readonly MirrorConfiguration _configuration;

	public Mirror() : this(new MirrorConfiguration()) { }

	public Mirror(MirrorConfiguration configuration)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	public TDestino Reflect<TOrigem, TDestino>(TOrigem origem) where TDestino : new()
	{
		ArgumentNullException.ThrowIfNull(origem);

		var key = (typeof(TOrigem), typeof(TDestino));
		if (_configuration.Factories.TryGetValue(key, out var factory))
			return (TDestino)factory(origem)!;

		var destino = new TDestino();
		var context = MappingContext.Create(_configuration.MaxDepth);
		MapearPropriedades(origem, destino, context);
		return destino;
	}

	public void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino)
	{
		ArgumentNullException.ThrowIfNull(origem);
		ArgumentNullException.ThrowIfNull(destino);

		var context = MappingContext.Create(_configuration.MaxDepth);
		MapearPropriedades(origem, destino, context);
	}

	public TDestino ReflectUsingFactory<TOrigem, TDestino>(TOrigem origem)
	{
		ArgumentNullException.ThrowIfNull(origem);

		var key = (typeof(TOrigem), typeof(TDestino));
		if (_configuration.Factories.TryGetValue(key, out var factory))
			return (TDestino)factory(origem)!;

		throw new InvalidOperationException($"Nenhuma factory registrada para {typeof(TOrigem).Name} -> {typeof(TDestino).Name}");
	}

	public TDestino ReflectWithFactory<TOrigem, TDestino>(TOrigem origem, Func<TOrigem, TDestino> factory)
	{
		ArgumentNullException.ThrowIfNull(origem);
		ArgumentNullException.ThrowIfNull(factory);

		var destino = factory(origem);
		var context = MappingContext.Create(_configuration.MaxDepth);
		MapearPropriedades(origem, destino, context);
		return destino;
	}

	private void MapearPropriedades<TOrigem, TDestino>(TOrigem origem, TDestino destino, MappingContext context)
	{
		var propriedadesOrigem = typeof(TOrigem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
		var propriedadesDestino = typeof(TDestino).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite && p.GetIndexParameters().Length == 0);
		var reflectionKey = (typeof(TOrigem), typeof(TDestino));

		foreach (var propDestino in propriedadesDestino)
		{
			try
			{
				Func<object, object?>? transform = null;
				var possuiTransformacao = _configuration.Transformations.TryGetValue(reflectionKey, out var transformations) &&
					transformations.TryGetValue(propDestino.Name, out transform);

				object? valorOrigem;
				if (possuiTransformacao)
				{
					valorOrigem = transform!(origem!);
				}
				else
				{
					if (!propriedadesOrigem.TryGetValue(propDestino.Name, out var propOrigem))
						continue;
					valorOrigem = propOrigem.GetValue(origem);
				}

				if (valorOrigem == null && _configuration.IgnoreNullValues)
					continue;

				if (IsDictionaryType(propDestino.PropertyType))
				{
					var dicionarioMapeado = MapearDicionario(valorOrigem, propDestino.PropertyType, context);
					if (dicionarioMapeado != null || !_configuration.IgnoreNullValues)
						propDestino.SetValue(destino, dicionarioMapeado);
				}
				else if (typeof(IEnumerable).IsAssignableFrom(propDestino.PropertyType) &&
					propDestino.PropertyType != typeof(string) &&
					propDestino.PropertyType != typeof(byte[]))
				{
					var listaMapeada = MapearLista(valorOrigem as IEnumerable, propDestino.PropertyType, context);
					if (listaMapeada != null || !_configuration.IgnoreNullValues)
						propDestino.SetValue(destino, listaMapeada);
				}
				else if (IsComplexType(propDestino.PropertyType))
				{
					if (valorOrigem == null)
					{
						if (!_configuration.IgnoreNullValues)
							propDestino.SetValue(destino, null);
						continue;
					}

					var destinoAtual = propDestino.GetValue(destino);
					var possuiFactory = _configuration.Factories.ContainsKey((valorOrigem.GetType(), propDestino.PropertyType));
					var objetoMapeado = !possuiFactory && destinoAtual != null && propDestino.PropertyType.IsInstanceOfType(destinoAtual)
						? MapearObjetoEmInstanciaExistente(valorOrigem, destinoAtual, propDestino.PropertyType, context.CreateChild())
						: MapearObjeto(valorOrigem, propDestino.PropertyType, context.CreateChild());
					if (objetoMapeado != null || !_configuration.IgnoreNullValues)
						propDestino.SetValue(destino, objetoMapeado);
				}
				else
				{
					propDestino.SetValue(destino, valorOrigem);
				}
			}
			catch (Exception ex)
			{
				throw new MirrorException($"Erro ao mapear propriedade '{propDestino.Name}'", ex);
			}
		}
	}

	private object? MapearObjeto(object origem, Type tipoDestino, MappingContext context)
	{
		if (context.MaxDepthReached || !context.TryEnter(origem))
			return null;

		try
		{
			var key = (origem.GetType(), tipoDestino);
			if (_configuration.Factories.TryGetValue(key, out var factory))
				return factory(origem);

			var destino = Activator.CreateInstance(tipoDestino);
			if (destino == null)
				return null;

			var metodoMapear = GetType().GetMethod(nameof(MapearPropriedades), BindingFlags.NonPublic | BindingFlags.Instance);
			var metodoGenerico = metodoMapear?.MakeGenericMethod(origem.GetType(), tipoDestino);
			metodoGenerico?.Invoke(this, new object[] { origem, destino, context.CreateChild() });

			return destino;
		}
		catch (TargetInvocationException ex)
		{
			throw ex.InnerException ?? ex;
		}
		finally
		{
			context.Exit(origem);
		}
	}

	private object? MapearObjetoEmInstanciaExistente(object origem, object destino, Type tipoDestino, MappingContext context)
	{
		if (context.MaxDepthReached || !context.TryEnter(origem))
			return destino;

		try
		{
			var metodoMapear = GetType().GetMethod(nameof(MapearPropriedades), BindingFlags.NonPublic | BindingFlags.Instance);
			var metodoGenerico = metodoMapear?.MakeGenericMethod(origem.GetType(), tipoDestino);
			metodoGenerico?.Invoke(this, new[] { origem, destino, context.CreateChild() });

			return destino;
		}
		catch (TargetInvocationException ex)
		{
			throw ex.InnerException ?? ex;
		}
		finally
		{
			context.Exit(origem);
		}
	}

	private object? MapearLista(IEnumerable? listaOrigem, Type tipoListaDestino, MappingContext context)
	{
		if (listaOrigem == null || context.MaxDepthReached)
			return null;

		if (tipoListaDestino == typeof(ArrayList))
		{
			var arrayList = new ArrayList();
			foreach (var item in listaOrigem)
			{
				arrayList.Add(item);
			}

			return arrayList;
		}

		Type tipoItem;
		if (tipoListaDestino.IsArray)
		{
			tipoItem = tipoListaDestino.GetElementType()!;
		}
		else if (tipoListaDestino.IsGenericType)
		{
			tipoItem = tipoListaDestino.GetGenericArguments()[0];
		}
		else
		{
			return null;
		}

		var itensMapeados = Activator.CreateInstance(typeof(List<>).MakeGenericType(tipoItem)) as IList;
		if (itensMapeados == null)
			return null;

		foreach (var item in listaOrigem)
		{
			if (item == null)
			{
				if (!tipoItem.IsValueType || Nullable.GetUnderlyingType(tipoItem) != null)
					itensMapeados.Add(null);
				continue;
			}

			if (IsSimpleType(tipoItem))
			{
				itensMapeados.Add(item);
				continue;
			}

			var itemMapeado = MapearObjeto(item, tipoItem, context.CreateChild());
			if (itemMapeado != null)
				itensMapeados.Add(itemMapeado);
		}

		if (tipoListaDestino.IsArray)
		{
			var array = Array.CreateInstance(tipoItem, itensMapeados.Count);
			for (var i = 0; i < itensMapeados.Count; i++)
			{
				array.SetValue(itensMapeados[i], i);
			}

			return array;
		}

		if (tipoListaDestino.IsAssignableFrom(itensMapeados.GetType()))
			return itensMapeados;

		if (TryCreateCollectionFromEnumerable(tipoListaDestino, tipoItem, itensMapeados, out var collection))
			return collection;

		return itensMapeados;
	}

	private object? MapearDicionario(object? dicionarioOrigem, Type tipoDicionarioDestino, MappingContext context)
	{
		if (dicionarioOrigem == null || context.MaxDepthReached)
			return null;

		if (TryMapearDicionarioNaoGenerico(dicionarioOrigem, tipoDicionarioDestino, context, out var dicionarioNaoGenerico))
			return dicionarioNaoGenerico;

		var genericArguments = tipoDicionarioDestino.GetGenericArguments();
		if (genericArguments.Length != 2)
			return null;

		var tipoChaveDestino = genericArguments[0];
		var tipoValorDestino = genericArguments[1];
		var tipoDictionary = typeof(Dictionary<,>).MakeGenericType(tipoChaveDestino, tipoValorDestino);
		var dicionarioTemporario = Activator.CreateInstance(tipoDictionary);
		if (dicionarioTemporario == null)
			return null;

		var addMethod = tipoDictionary.GetMethod("Add", new[] { tipoChaveDestino, tipoValorDestino });
		if (addMethod == null)
			return null;

		foreach (var (chaveOrigem, valorOrigem) in EnumerarEntradasDeDicionario(dicionarioOrigem))
		{
			var chaveMapeada = MapearValorDeDicionario(chaveOrigem, tipoChaveDestino, context.CreateChild());
			var valorMapeado = MapearValorDeDicionario(valorOrigem, tipoValorDestino, context.CreateChild());

			if (chaveMapeada == null && tipoChaveDestino.IsValueType && Nullable.GetUnderlyingType(tipoChaveDestino) == null)
				continue;

			if (valorMapeado == null && tipoValorDestino.IsValueType && Nullable.GetUnderlyingType(tipoValorDestino) == null)
				continue;

			addMethod.Invoke(dicionarioTemporario, new[] { chaveMapeada, valorMapeado });
		}

		if (tipoDicionarioDestino.IsAssignableFrom(tipoDictionary))
			return dicionarioTemporario;

		if (TryCreateDictionaryFromEnumerable(tipoDicionarioDestino, tipoChaveDestino, tipoValorDestino, dicionarioTemporario, out var dicionarioFinal))
			return dicionarioFinal;

		return dicionarioTemporario;
	}

	private bool TryMapearDicionarioNaoGenerico(object dicionarioOrigem, Type tipoDicionarioDestino, MappingContext context, out object? dicionario)
	{
		dicionario = null;

		if (tipoDicionarioDestino.IsGenericType)
			return false;

		if (tipoDicionarioDestino == typeof(IDictionary) || tipoDicionarioDestino == typeof(Hashtable))
		{
			var hashtable = new Hashtable();
			foreach (var (key, value) in EnumerarEntradasDeDicionario(dicionarioOrigem))
			{
				hashtable[key!] = value;
			}

			dicionario = hashtable;
			return true;
		}

		if (!typeof(IDictionary).IsAssignableFrom(tipoDicionarioDestino))
			return false;

		var instancia = Activator.CreateInstance(tipoDicionarioDestino) as IDictionary;
		if (instancia == null)
			return false;

		foreach (var (key, value) in EnumerarEntradasDeDicionario(dicionarioOrigem))
		{
			instancia[key!] = value;
		}

		dicionario = instancia;
		return true;
	}

	private static bool TryCreateCollectionFromEnumerable(Type tipoListaDestino, Type tipoItem, IList itensMapeados, out object? collection)
	{
		collection = null;

		if (TryCreateImmutableCollection(tipoListaDestino, tipoItem, itensMapeados, out collection))
			return true;

		var constructor = tipoListaDestino
			.GetConstructors()
			.OrderBy(c => c.GetParameters().Length)
			.FirstOrDefault(c =>
			{
				var parameters = c.GetParameters();
				if (parameters.Length != 1)
					return false;

				return parameters[0].ParameterType.IsAssignableFrom(itensMapeados.GetType());
			});

		if (constructor != null)
		{
			collection = constructor.Invoke(new[] { itensMapeados });
			return true;
		}

		var parameterlessConstructor = tipoListaDestino.GetConstructor(Type.EmptyTypes);
		var addMethod = tipoListaDestino.GetMethod("Add", new[] { tipoItem });
		if (parameterlessConstructor != null && addMethod != null)
		{
			collection = parameterlessConstructor.Invoke(null);
			foreach (var item in itensMapeados)
			{
				addMethod.Invoke(collection, new[] { item });
			}
			return true;
		}

		return false;
	}

	private static bool TryCreateDictionaryFromEnumerable(
		Type tipoDicionarioDestino,
		Type tipoChave,
		Type tipoValor,
		object dicionarioTemporario,
		out object? dictionary)
	{
		dictionary = null;

		if (TryCreateImmutableDictionary(tipoDicionarioDestino, tipoChave, tipoValor, dicionarioTemporario, out dictionary))
			return true;

		var constructor = tipoDicionarioDestino
			.GetConstructors()
			.OrderBy(c => c.GetParameters().Length)
			.FirstOrDefault(c =>
			{
				var parameters = c.GetParameters();
				if (parameters.Length != 1)
					return false;

				return parameters[0].ParameterType.IsAssignableFrom(dicionarioTemporario.GetType());
			});

		if (constructor != null)
		{
			dictionary = constructor.Invoke(new[] { dicionarioTemporario });
			return true;
		}

		var parameterlessConstructor = tipoDicionarioDestino.GetConstructor(Type.EmptyTypes);
		var addMethod = tipoDicionarioDestino.GetMethod("Add", new[] { tipoChave, tipoValor });
		if (parameterlessConstructor != null && addMethod != null)
		{
			dictionary = parameterlessConstructor.Invoke(null);
			foreach (var (key, value) in EnumerarEntradasDeDicionario(dicionarioTemporario))
			{
				addMethod.Invoke(dictionary, new[] { key, value });
			}
			return true;
		}

		return false;
	}

	private static bool TryCreateImmutableDictionary(
		Type tipoDicionarioDestino,
		Type tipoChave,
		Type tipoValor,
		object dicionarioTemporario,
		out object? dictionary)
	{
		dictionary = null;

		if (!tipoDicionarioDestino.IsGenericType)
			return false;

		var genericDefinition = tipoDicionarioDestino.GetGenericTypeDefinition();
		if (genericDefinition.FullName != "System.Collections.Immutable.ImmutableDictionary`2")
			return false;

		var immutableDictionaryType = Type.GetType("System.Collections.Immutable.ImmutableDictionary, System.Collections.Immutable");
		var createRangeMethod = immutableDictionaryType?
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m =>
				m.Name == "CreateRange" &&
				m.IsGenericMethod &&
				m.GetParameters().Length == 1);

		if (createRangeMethod == null)
			return false;

		dictionary = createRangeMethod.MakeGenericMethod(tipoChave, tipoValor).Invoke(null, new[] { dicionarioTemporario });
		return dictionary != null;
	}

	private static bool TryCreateImmutableCollection(Type tipoListaDestino, Type tipoItem, IList itensMapeados, out object? collection)
	{
		collection = null;

		if (!tipoListaDestino.IsGenericType)
			return false;

		var genericDefinition = tipoListaDestino.GetGenericTypeDefinition();
		if (genericDefinition.FullName != "System.Collections.Immutable.ImmutableList`1")
			return false;

		var immutableListType = Type.GetType("System.Collections.Immutable.ImmutableList, System.Collections.Immutable");
		var createRangeMethod = immutableListType?
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == "CreateRange" && m.IsGenericMethod && m.GetParameters().Length == 1);

		if (createRangeMethod == null)
			return false;

		collection = createRangeMethod.MakeGenericMethod(tipoItem).Invoke(null, new[] { itensMapeados });
		return collection != null;
	}

	private object? MapearValorDeDicionario(object? valorOrigem, Type tipoDestino, MappingContext context)
	{
		if (valorOrigem == null)
			return null;

		if (tipoDestino.IsInstanceOfType(valorOrigem))
			return valorOrigem;

		if (IsDictionaryType(tipoDestino))
			return MapearDicionario(valorOrigem, tipoDestino, context);

		if (typeof(IEnumerable).IsAssignableFrom(tipoDestino) &&
			tipoDestino != typeof(string) &&
			tipoDestino != typeof(byte[]))
		{
			return MapearLista(valorOrigem as IEnumerable, tipoDestino, context);
		}

		if (IsSimpleType(tipoDestino))
		{
			try
			{
				return Convert.ChangeType(valorOrigem, tipoDestino);
			}
			catch
			{
				return valorOrigem;
			}
		}

		return MapearObjeto(valorOrigem, tipoDestino, context);
	}

	private static IEnumerable<(object? Key, object? Value)> EnumerarEntradasDeDicionario(object origem)
	{
		if (origem is IDictionary dictionary)
		{
			foreach (DictionaryEntry entry in dictionary)
			{
				yield return (entry.Key, entry.Value);
			}

			yield break;
		}

		if (origem is not IEnumerable enumerable)
			yield break;

		foreach (var item in enumerable)
		{
			if (item == null)
				continue;

			var itemType = item.GetType();
			var keyProperty = itemType.GetProperty("Key");
			var valueProperty = itemType.GetProperty("Value");
			if (keyProperty == null || valueProperty == null)
				continue;

			yield return (keyProperty.GetValue(item), valueProperty.GetValue(item));
		}
	}

	private static bool IsDictionaryType(Type type)
	{
		if (type == typeof(IDictionary) ||
			type == typeof(Hashtable) ||
			(!type.IsGenericType && typeof(IDictionary).IsAssignableFrom(type)))
			return true;

		if (!type.IsGenericType)
			return false;

		var genericDefinition = type.GetGenericTypeDefinition();
		return genericDefinition == typeof(Dictionary<,>) ||
			genericDefinition == typeof(IDictionary<,>) ||
			genericDefinition == typeof(IReadOnlyDictionary<,>) ||
			genericDefinition == typeof(SortedList<,>) ||
			genericDefinition == typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>) ||
			genericDefinition == typeof(System.Collections.Concurrent.ConcurrentDictionary<,>) ||
			genericDefinition.FullName == "System.Collections.Immutable.ImmutableDictionary`2";
	}

	private static bool IsComplexType(Type type)
	{
		return type.IsClass && type != typeof(string);
	}

	private static bool IsSimpleType(Type type)
	{
		return type.IsPrimitive ||
			type == typeof(string) ||
			type == typeof(Guid) ||
			type.IsEnum ||
			type.IsValueType;
	}

	private sealed class MappingContext
	{
		private readonly HashSet<object> _path;

		private MappingContext(int depth, int maxDepth, HashSet<object> path)
		{
			Depth = depth;
			MaxDepth = maxDepth;
			_path = path;
		}

		public int Depth { get; }
		public int MaxDepth { get; }
		public bool MaxDepthReached => Depth > MaxDepth;

		public static MappingContext Create(int maxDepth)
		{
			return new MappingContext(0, Math.Max(0, maxDepth), new HashSet<object>(ReferenceEqualityComparer.Instance));
		}

		public MappingContext CreateChild()
		{
			return new MappingContext(Depth + 1, MaxDepth, _path);
		}

		public bool TryEnter(object? instance)
		{
			if (instance == null || !RequiresTracking(instance.GetType()))
				return true;

			return _path.Add(instance);
		}

		public void Exit(object? instance)
		{
			if (instance == null || !RequiresTracking(instance.GetType()))
				return;

			_path.Remove(instance);
		}

		private static bool RequiresTracking(Type type)
		{
			return !type.IsValueType && type != typeof(string);
		}
	}
}
