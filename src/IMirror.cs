namespace Mirror;

public interface IMirror
{
	// Versão com constraint new() - para tipos com construtor público
	TDestino Reflect<TOrigem, TDestino>(TOrigem origem) where TDestino : new();

	// Versão sem constraint - para tipos que usam factory
	TDestino ReflectWithFactory<TOrigem, TDestino>(TOrigem origem, Func<TOrigem, TDestino> factory);

	// NOVO: Versão sem constraint que usa factories registradas
	TDestino ReflectUsingFactory<TOrigem, TDestino>(TOrigem origem);

	void Reflect<TOrigem, TDestino>(TOrigem origem, TDestino destino);
}