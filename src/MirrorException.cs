namespace Mirror.Exceptions;

public class MirrorException : Exception
{
	public MirrorException(string message, Exception innerException) : base(message, innerException) { }
}
