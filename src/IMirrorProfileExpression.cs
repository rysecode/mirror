using System.Linq.Expressions;

namespace Mirror;

public interface IMirrorProfileExpression
{
	IMirrorProfileExpression CreateReflection<TOrigem, TDestino>();
	IMirrorProfileExpression ForMember<TOrigem, TDestino, TProp>(
		Expression<Func<TDestino, TProp>> destinoMember,
		Func<TOrigem, object> transform);

	IMirrorProfileExpression UseFactory<TOrigem, TDestino>(Func<TOrigem, TDestino> factory);
}
