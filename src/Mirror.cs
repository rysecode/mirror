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
		if (origem is null)
			throw new ArgumentNullException(nameof(origem));

		Console.WriteLine($"Reflect: {typeof(TOrigem).Name} -> {typeof(TDestino).Name}");

		// Verifica se existe factory registrada - ANTES de qualquer outra coisa
		var key = (typeof(TOrigem), typeof(TDestino));
		if (_configuration.Factories.TryGetValue(key, out var factory))
		{
			Console.WriteLine("  Usando factory registrada");
			try
			{
				return ((Func<TOrigem, TDestino>)factory)(origem);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"  Erro na factory: {ex.Message}");
				throw; // Relança a exceção original
			}
		}

		Console.WriteLine("  Sem factory, criando com new()");
		var destino = new TDestino();
		MapearPropriedades(origem, destino);
		return destino;
	}

	public void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino)
	{
		if (origem is null)
			throw new ArgumentNullException(nameof(origem));
		if (destino is null)
			throw new ArgumentNullException(nameof(destino));

		MapearPropriedades(origem, destino);
	}

	public TDestino ReflectUsingFactory<TOrigem, TDestino>(TOrigem origem)
	{
		if (origem is null)
			throw new ArgumentNullException(nameof(origem));

		var key = (typeof(TOrigem), typeof(TDestino));
		if (_configuration.Factories.TryGetValue(key, out var factory))
		{
			// Não encapsula a exceção - deixa ela passar diretamente
			return ((Func<TOrigem, TDestino>)factory)(origem);
		}

		throw new InvalidOperationException($"Nenhuma factory registrada para {typeof(TOrigem).Name} -> {typeof(TDestino).Name}");
	}

	public TDestino ReflectWithFactory<TOrigem, TDestino>(TOrigem origem, Func<TOrigem, TDestino> factory)
	{
		if (origem is null)
			throw new ArgumentNullException(nameof(origem));
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));

		var destino = factory(origem);
		MapearPropriedades(origem, destino);
		return destino;
	}

	private void MapearPropriedades<TOrigem, TDestino>(TOrigem origem, TDestino destino)
	{
		var propriedadesOrigem = typeof(TOrigem).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		var propriedadesDestino = typeof(TDestino).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		foreach (var propOrigem in propriedadesOrigem)
		{
			try
			{
				if (!propriedadesDestino.TryGetValue(propOrigem.Name, out var propDestino))
					continue;

				if (propDestino.GetIndexParameters().Length > 0)
					continue;

				var valorOrigem = propOrigem.GetValue(origem);

				// Aplica transformações se houver
				var key = (typeof(TOrigem), typeof(TDestino));
				if (_configuration.Transformations.TryGetValue(key, out var transformations) &&
					transformations.TryGetValue(propDestino.Name, out var transform))
				{
					valorOrigem = transform(valorOrigem);
				}

				if (valorOrigem == null && _configuration.IgnoreNullValues)
					continue;

				// CASO 1: É uma lista/coleção
				if (typeof(IEnumerable).IsAssignableFrom(propDestino.PropertyType) &&
					propDestino.PropertyType != typeof(string) &&
					propDestino.PropertyType != typeof(byte[]))
				{
					var listaMapeada = MapearLista(valorOrigem as IEnumerable, propDestino.PropertyType);
					propDestino.SetValue(destino, listaMapeada);
				}
				// CASO 2: É um objeto complexo (classe)
				else if (propDestino.PropertyType.IsClass &&
						 propDestino.PropertyType != typeof(string) &&
						 !propDestino.PropertyType.IsValueType)
				{
					if (valorOrigem != null)
					{
						var objetoMapeado = MapearObjeto(valorOrigem, propDestino.PropertyType);
						propDestino.SetValue(destino, objetoMapeado);
					}
				}
				// CASO 3: É tipo simples
				else
				{
					propDestino.SetValue(destino, valorOrigem);
				}
			}
			catch (Exception ex)
			{
				throw new MirrorException($"Erro ao mapear propriedade '{propOrigem.Name}'", ex);
			}
		}
	}

	private object? MapearObjeto(object origem, Type tipoDestino)
	{
		try
		{
			// Verifica se existe factory para este tipo
			var key = (origem.GetType(), tipoDestino);
			if (_configuration.Factories.TryGetValue(key, out var factory))
			{
				return ((Func<object, object>)factory)(origem);
			}

			var destino = Activator.CreateInstance(tipoDestino);
			if (destino == null) return null;

			var metodoMapear = GetType().GetMethod(nameof(MapearPropriedades), BindingFlags.NonPublic | BindingFlags.Instance);
			var metodoGenerico = metodoMapear?.MakeGenericMethod(origem.GetType(), tipoDestino);
			metodoGenerico?.Invoke(this, new[] { origem, destino });

			return destino;
		}
		catch
		{
			return origem;
		}
	}

	private object? MapearLista(IEnumerable? listaOrigem, Type tipoListaDestino)
	{
		if (listaOrigem == null) return null;

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
			return listaOrigem;
		}

		var listaDestino = Activator.CreateInstance(typeof(List<>).MakeGenericType(tipoItem)) as System.Collections.IList;
		if (listaDestino == null) return listaOrigem;

		foreach (var item in listaOrigem)
		{
			if (item == null) continue;

			if (tipoItem.IsPrimitive ||
				tipoItem == typeof(string) ||
				tipoItem == typeof(Guid) ||
				tipoItem.IsEnum ||
				tipoItem.IsValueType)
			{
				listaDestino.Add(item);
			}
			else
			{
				var itemMapeado = MapearObjeto(item, tipoItem);
				listaDestino.Add(itemMapeado ?? item);
			}
		}

		if (tipoListaDestino.IsArray)
		{
			var array = Array.CreateInstance(tipoItem, listaDestino.Count);
			for (int i = 0; i < listaDestino.Count; i++)
			{
				array.SetValue(listaDestino[i], i);
			}
			return array;
		}

		return listaDestino;
	}
}