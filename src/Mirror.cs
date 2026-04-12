using Mirror.Exceptions;
using System.Collections;
using System.Linq.Expressions;
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
		return Reflect<TOrigem, TDestino>(origem, Array.Empty<Expression<Func<TDestino, object?>>>());
	}

	public TDestino Reflect<TOrigem, TDestino>(TOrigem origem, params Expression<Func<TDestino, object?>>[] ignoreMembers) where TDestino : new()
	{
		ArgumentNullException.ThrowIfNull(origem);

		var key = (typeof(TOrigem), typeof(TDestino));
		if (_configuration.Factories.TryGetValue(key, out var factory))
			return (TDestino)factory(origem)!;

		var destino = new TDestino();
		var context = MappingContext.Create(_configuration.MaxDepth);
		MapearPropriedades(origem, destino, context, CreateIgnoreSet(ignoreMembers));
		return destino;
	}

	public void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino)
	{
		Reflect(origem, destino, Array.Empty<Expression<Func<TDestino, object?>>>());
	}

	public void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino, params Expression<Func<TDestino, object?>>[] ignoreMembers)
	{
		ArgumentNullException.ThrowIfNull(origem);
		ArgumentNullException.ThrowIfNull(destino);

		var context = MappingContext.Create(_configuration.MaxDepth);
		MapearPropriedades(origem, destino, context, CreateIgnoreSet(ignoreMembers));
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
		MapearPropriedades(origem, destino, context, []);
		return destino;
	}

	private void MapearPropriedades<TOrigem, TDestino>(TOrigem origem, TDestino destino, MappingContext context, HashSet<string> ignoredMembers)
	{
		var propriedadesOrigem = typeof(TOrigem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
		var propriedadesDestino = typeof(TDestino).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite && p.GetIndexParameters().Length == 0);
		var reflectionKey = (typeof(TOrigem), typeof(TDestino));

		foreach (var propDestino in propriedadesDestino)
		{
			PropertyInfo? propOrigem = null;
			object? valorOrigem = null;
			var propertyContext = context.CreateProperty(propDestino.Name);

			try
			{
				if (ShouldIgnoreProperty(propDestino, ignoredMembers))
					continue;

				Func<object, object?>? transform = null;
				var possuiTransformacao = _configuration.Transformations.TryGetValue(reflectionKey, out var transformations) &&
					transformations.TryGetValue(propDestino.Name, out transform);

				if (possuiTransformacao)
				{
					valorOrigem = transform!(origem!);
				}
				else
				{
					if (!propriedadesOrigem.TryGetValue(propDestino.Name, out var propertyFromSource))
						continue;

					propOrigem = propertyFromSource;
					valorOrigem = propertyFromSource.GetValue(origem);
				}

				if (valorOrigem == null && _configuration.IgnoreNullValues)
					continue;

				if (IsDictionaryType(propDestino.PropertyType))
				{
					var dicionarioMapeado = MapearDicionario(valorOrigem, propDestino.PropertyType, propertyContext);
					if (dicionarioMapeado != null || !_configuration.IgnoreNullValues)
						propDestino.SetValue(destino, dicionarioMapeado);
				}
				else if (typeof(IEnumerable).IsAssignableFrom(propDestino.PropertyType) &&
					propDestino.PropertyType != typeof(string) &&
					propDestino.PropertyType != typeof(byte[]))
				{
					var listaMapeada = MapearLista(valorOrigem as IEnumerable, propDestino.PropertyType, propertyContext);
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
						? MapearObjetoEmInstanciaExistente(valorOrigem, destinoAtual, propDestino.PropertyType, propertyContext.CreateChild(), ignoredMembers)
						: MapearObjeto(valorOrigem, propDestino.PropertyType, propertyContext.CreateChild(), ignoredMembers);
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
				throw CreatePropertyMappingException(
					ex,
					typeof(TOrigem),
					typeof(TDestino),
					propOrigem,
					propDestino,
					valorOrigem,
					propertyContext,
					"PropertyMapping");
			}
		}
	}

	private object? MapearObjeto(object origem, Type tipoDestino, MappingContext context)
	{
		return MapearObjeto(origem, tipoDestino, context, []);
	}

	private object? MapearObjeto(object origem, Type tipoDestino, MappingContext context, HashSet<string> ignoredMembers)
	{
		if (context.MaxDepthReached || !context.TryEnter(origem))
			return null;

		try
		{
			var key = (origem.GetType(), tipoDestino);
			if (_configuration.Factories.TryGetValue(key, out var factory))
			{
				try
				{
					return factory(origem);
				}
				catch (Exception ex)
				{
					throw CreateOperationException(ex, "Factory", context, origem.GetType(), tipoDestino, sourceValue: origem);
				}
			}

			var destino = Activator.CreateInstance(tipoDestino);
			if (destino == null)
				return null;

			var metodoMapear = GetType().GetMethod(nameof(MapearPropriedades), BindingFlags.NonPublic | BindingFlags.Instance);
			var metodoGenerico = metodoMapear?.MakeGenericMethod(origem.GetType(), tipoDestino);
			metodoGenerico?.Invoke(this, new object[] { origem, destino, context.CreateChild(), ignoredMembers });

			return destino;
		}
		catch (TargetInvocationException ex)
		{
			throw ex.InnerException ?? ex;
		}
		catch (MirrorException)
		{
			throw;
		}
		catch (Exception ex)
		{
			throw CreateOperationException(ex, "ObjectMapping", context, origem.GetType(), tipoDestino, sourceValue: origem);
		}
		finally
		{
			context.Exit(origem);
		}
	}

	private object? MapearObjetoEmInstanciaExistente(object origem, object destino, Type tipoDestino, MappingContext context, HashSet<string> ignoredMembers)
	{
		if (context.MaxDepthReached || !context.TryEnter(origem))
			return destino;

		try
		{
			var metodoMapear = GetType().GetMethod(nameof(MapearPropriedades), BindingFlags.NonPublic | BindingFlags.Instance);
			var metodoGenerico = metodoMapear?.MakeGenericMethod(origem.GetType(), tipoDestino);
			metodoGenerico?.Invoke(this, new object[] { origem, destino, context.CreateChild(), ignoredMembers });

			return destino;
		}
		catch (TargetInvocationException ex)
		{
			throw ex.InnerException ?? ex;
		}
		catch (MirrorException)
		{
			throw;
		}
		catch (Exception ex)
		{
			throw CreateOperationException(ex, "ExistingObjectMapping", context, origem.GetType(), tipoDestino, sourceValue: origem);
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
			var itemContext = context.CreateCollectionItem(itensMapeados.Count);
			if (item == null)
			{
				if (!tipoItem.IsValueType || Nullable.GetUnderlyingType(tipoItem) != null)
					itensMapeados.Add(null);
				continue;
			}

			if (IsSimpleType(tipoItem))
			{
				try
				{
					itensMapeados.Add(item);
				}
				catch (Exception ex)
				{
					throw CreateOperationException(ex, "CollectionItemAssignment", itemContext, item.GetType(), tipoItem, sourceValue: item);
				}
				continue;
			}

			var itemMapeado = MapearObjeto(item, tipoItem, itemContext.CreateChild(), []);
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
			var keyContext = context.CreateDictionaryKey(chaveOrigem);
			var valueContext = context.CreateDictionaryValue(chaveOrigem);
			var chaveMapeada = MapearValorDeDicionario(chaveOrigem, tipoChaveDestino, keyContext.CreateChild());
			var valorMapeado = MapearValorDeDicionario(valorOrigem, tipoValorDestino, valueContext.CreateChild());

			if (chaveMapeada == null && tipoChaveDestino.IsValueType && Nullable.GetUnderlyingType(tipoChaveDestino) == null)
				continue;

			if (valorMapeado == null && tipoValorDestino.IsValueType && Nullable.GetUnderlyingType(tipoValorDestino) == null)
				continue;

			try
			{
				addMethod.Invoke(dicionarioTemporario, new[] { chaveMapeada, valorMapeado });
			}
			catch (Exception ex)
			{
				throw CreateOperationException(ex, "DictionaryAdd", valueContext, chaveOrigem?.GetType(), tipoValorDestino, sourceValue: valorOrigem);
			}
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
			catch (Exception ex)
			{
				throw CreateOperationException(ex, "ValueConversion", context, valorOrigem.GetType(), tipoDestino, sourceValue: valorOrigem);
			}
		}

		return MapearObjeto(valorOrigem, tipoDestino, context, []);
	}

	private static HashSet<string> CreateIgnoreSet<TDestino>(IEnumerable<Expression<Func<TDestino, object?>>>? ignoreMembers)
	{
		var ignoredMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (ignoreMembers == null)
			return ignoredMembers;

		foreach (var ignoreMember in ignoreMembers)
		{
			var propertyName = GetPropertyName(ignoreMember);
			if (!string.IsNullOrWhiteSpace(propertyName))
				ignoredMembers.Add(propertyName);
		}

		return ignoredMembers;
	}

	private static string? GetPropertyName<TDestino>(Expression<Func<TDestino, object?>> expression)
	{
		return expression.Body switch
		{
			MemberExpression member => member.Member.Name,
			UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
			_ => throw new ArgumentException("A expressão deve apontar para uma propriedade.", nameof(expression))
		};
	}

	private static bool ShouldIgnoreProperty(PropertyInfo propertyInfo, HashSet<string> ignoredMembers)
	{
		return ignoredMembers.Contains(propertyInfo.Name) ||
			propertyInfo.IsDefined(typeof(MirrorNonReflectAttribute), inherit: true);
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

	private static MirrorException CreatePropertyMappingException(
		Exception exception,
		Type sourceType,
		Type destinationType,
		PropertyInfo? sourceProperty,
		PropertyInfo destinationProperty,
		object? sourceValue,
		MappingContext context,
		string stage)
	{
		var sourceMember = sourceProperty == null
			? "transformação customizada"
			: $"{sourceProperty.Name} ({GetFriendlyTypeName(sourceProperty.PropertyType)})";
		var destinationMember = $"{destinationProperty.Name} ({GetFriendlyTypeName(destinationProperty.PropertyType)})";
		var message =
			$"Erro ao mapear a propriedade '{destinationProperty.Name}' em '{context.Path}'. " +
			$"Origem: {GetFriendlyTypeName(sourceType)}.{sourceMember}. " +
			$"Destino: {GetFriendlyTypeName(destinationType)}.{destinationMember}. " +
			$"Etapa: {stage}. " +
			$"Valor atual: {FormatValue(sourceValue)}.";

		return new MirrorException(
			message,
			exception,
			stage,
			context.Path,
			sourceType,
			destinationType,
			destinationProperty.Name,
			sourceProperty?.PropertyType,
			destinationProperty.PropertyType,
			sourceValue);
	}

	private static MirrorException CreateOperationException(
		Exception exception,
		string stage,
		MappingContext context,
		Type? sourceType = null,
		Type? destinationType = null,
		string? memberName = null,
		Type? sourceMemberType = null,
		Type? destinationMemberType = null,
		object? sourceValue = null)
	{
		var message =
			$"Erro durante a etapa '{stage}' em '{context.Path}'. " +
			$"Origem: {GetFriendlyTypeName(sourceType)}. " +
			$"Destino: {GetFriendlyTypeName(destinationType)}. " +
			$"Valor atual: {FormatValue(sourceValue)}.";

		return new MirrorException(
			message,
			exception,
			stage,
			context.Path,
			sourceType,
			destinationType,
			memberName,
			sourceMemberType,
			destinationMemberType,
			sourceValue);
	}

	private static string GetFriendlyTypeName(Type? type)
	{
		if (type == null)
			return "(desconhecido)";

		if (!type.IsGenericType)
			return type.Name;

		var genericName = type.Name[..type.Name.IndexOf('`')];
		var genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
		return $"{genericName}<{genericArguments}>";
	}

	private static string FormatValue(object? value)
	{
		if (value == null)
			return "null";

		return value switch
		{
			string text => $"\"{text}\"",
			_ => $"{value} ({GetFriendlyTypeName(value.GetType())})"
		};
	}

	private sealed class MappingContext
	{
		private readonly HashSet<object> _path;

		private MappingContext(int depth, int maxDepth, HashSet<object> path, string currentPath)
		{
			Depth = depth;
			MaxDepth = maxDepth;
			_path = path;
			Path = currentPath;
		}

		public int Depth { get; }
		public int MaxDepth { get; }
		public string Path { get; }
		public bool MaxDepthReached => Depth > MaxDepth;

		public static MappingContext Create(int maxDepth)
		{
			return new MappingContext(0, Math.Max(0, maxDepth), new HashSet<object>(ReferenceEqualityComparer.Instance), "$");
		}

		public MappingContext CreateChild()
		{
			return new MappingContext(Depth + 1, MaxDepth, _path, Path);
		}

		public MappingContext CreateProperty(string propertyName)
		{
			var nextPath = Path == "$" ? propertyName : $"{Path}.{propertyName}";
			return new MappingContext(Depth, MaxDepth, _path, nextPath);
		}

		public MappingContext CreateCollectionItem(int index)
		{
			return new MappingContext(Depth, MaxDepth, _path, $"{Path}[{index}]");
		}

		public MappingContext CreateDictionaryKey(object? key)
		{
			return new MappingContext(Depth, MaxDepth, _path, $"{Path}[key:{FormatPathToken(key)}]");
		}

		public MappingContext CreateDictionaryValue(object? key)
		{
			return new MappingContext(Depth, MaxDepth, _path, $"{Path}[{FormatPathToken(key)}]");
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

		private static string FormatPathToken(object? token)
		{
			return token?.ToString() ?? "null";
		}
	}
}
