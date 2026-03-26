namespace Mirror;

public abstract class MirrorProfile : IMirrorProfile
{
#pragma warning disable CS8618 // O campo não anulável precisa conter um valor não nulo ao sair do construtor. Considere adicionar o modificador "obrigatório" ou declarar como anulável.
	protected IMirrorProfileExpression Expression { get; private set; }
#pragma warning restore CS8618 // O campo não anulável precisa conter um valor não nulo ao sair do construtor. Considere adicionar o modificador "obrigatório" ou declarar como anulável.

	public abstract void Configure(IMirrorProfileExpression expression);

	void IMirrorProfile.Configure(IMirrorProfileExpression expression)
	{
		Expression = expression;
		Configure(expression);
	}
}
