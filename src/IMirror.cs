using System.Linq.Expressions;

namespace Mirror;

public interface IMirror
{
	TDestino Reflect<TOrigem, TDestino>(TOrigem origem) where TDestino : new();
	TDestino Reflect<TOrigem, TDestino>(TOrigem origem, params Expression<Func<TDestino, object?>>[] ignoreMembers) where TDestino : new();

	TDestino ReflectWithFactory<TOrigem, TDestino>(TOrigem origem, Func<TOrigem, TDestino> factory);

	TDestino ReflectUsingFactory<TOrigem, TDestino>(TOrigem origem);

	void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino);
	void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino, params Expression<Func<TDestino, object?>>[] ignoreMembers);
}
