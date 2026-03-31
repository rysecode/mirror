using System.Linq.Expressions;

namespace Mirror;

public class MirrorConfiguration
{
	internal Dictionary<(Type, Type), Dictionary<string, Func<object, object?>>> Transformations { get; } = new();

	internal Dictionary<(Type, Type), Func<object, object?>> Factories { get; } = new();

	public bool IgnoreNullValues { get; set; } = false;

	public int MaxDepth { get; set; } = 5;

	public IReflectionExpression<TOrigem, TDestino> CreateReflection<TOrigem, TDestino>()
	{
		return new ReflectionExpression<TOrigem, TDestino>(this);
	}

	public void AddFactory<TOrigem, TDestino>(Func<TOrigem, TDestino> factory)
	{
		ArgumentNullException.ThrowIfNull(factory);

		Factories[(typeof(TOrigem), typeof(TDestino))] = origem => factory((TOrigem)origem);
	}
}

public interface IReflectionExpression<TOrigem, TDestino>
{
	IReflectionExpression<TOrigem, TDestino> ForMember<TProp>(
		Expression<Func<TDestino, TProp>> destinoMember,
		Func<TOrigem, object> transform);

	IReflectionExpression<TOrigem, TDestino> UseFactory(Func<TOrigem, TDestino> factory);
}

public class ReflectionExpression<TOrigem, TDestino> : IReflectionExpression<TOrigem, TDestino>
{
	private readonly MirrorConfiguration _configuration;

	public ReflectionExpression(MirrorConfiguration configuration)
	{
		_configuration = configuration;
	}

	public IReflectionExpression<TOrigem, TDestino> ForMember<TProp>(
		Expression<Func<TDestino, TProp>> destinoMember,
		Func<TOrigem, object> transform)
	{
		if (destinoMember.Body is MemberExpression memberExpr)
		{
			var key = (typeof(TOrigem), typeof(TDestino));

			if (!_configuration.Transformations.ContainsKey(key))
				_configuration.Transformations[key] = new Dictionary<string, Func<object, object?>>();

			var propertyName = memberExpr.Member.Name;
			_configuration.Transformations[key][propertyName] = obj => transform((TOrigem)obj);
		}

		return this;
	}

	public IReflectionExpression<TOrigem, TDestino> UseFactory(Func<TOrigem, TDestino> factory)
	{
		_configuration.AddFactory(factory);
		return this;
	}
}
