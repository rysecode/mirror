using System.Linq.Expressions;

namespace Mirror;

public class MirrorProfileExpression(MirrorConfiguration configuration) : IMirrorProfileExpression
{
	private readonly MirrorConfiguration _configuration = configuration;
#pragma warning disable CS8618 // O campo não anulável precisa conter um valor não nulo ao sair do construtor. Considere adicionar o modificador "obrigatório" ou declarar como anulável.
	private Type _currentOrigem;
	private Type _currentDestino;
#pragma warning restore CS8618 // O campo não anulável precisa conter um valor não nulo ao sair do construtor. Considere adicionar o modificador "obrigatório" ou declarar como anulável.

	public IMirrorProfileExpression CreateReflection<TOrigem, TDestino>()
	{
		_currentOrigem = typeof(TOrigem);
		_currentDestino = typeof(TDestino);
		return this;
	}

	public IMirrorProfileExpression ForMember<TOrigem, TDestino, TProp>(
		Expression<Func<TDestino, TProp>> destinoMember,
		Func<TOrigem, object> transform)
	{
		var key = (typeof(TOrigem), typeof(TDestino));

		if (!_configuration.Transformations.ContainsKey(key))
			_configuration.Transformations[key] = new Dictionary<string, Func<object, object>>();

		if (destinoMember.Body is MemberExpression memberExpr)
		{
			var propertyName = memberExpr.Member.Name;
			_configuration.Transformations[key][propertyName] = obj => transform((TOrigem)obj);
		}

		return this;
	}

	public IMirrorProfileExpression UseFactory<TOrigem, TDestino>(Func<TOrigem, TDestino> factory)
	{
		// Verifica se os tipos correspondem aos do CreateReflection
		if (typeof(TOrigem) != _currentOrigem || typeof(TDestino) != _currentDestino)
		{
			throw new InvalidOperationException(
				$"Os tipos no UseFactory devem ser os mesmos do CreateReflection. " +
				$"Esperado: {_currentOrigem.Name} -> {_currentDestino.Name}. " +
				$"Recebido: {typeof(TOrigem).Name} -> {typeof(TDestino).Name}");
		}

		_configuration.AddFactory(factory);
		return this;
	}
}