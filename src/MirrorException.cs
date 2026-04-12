namespace Mirror.Exceptions;

public class MirrorException : Exception
{
	public MirrorException(
		string message,
		Exception innerException,
		string? stage = null,
		string? path = null,
		Type? sourceType = null,
		Type? destinationType = null,
		string? memberName = null,
		Type? sourceMemberType = null,
		Type? destinationMemberType = null,
		object? sourceValue = null)
		: base(message, innerException)
	{
		Stage = stage;
		Path = path;
		SourceType = sourceType;
		DestinationType = destinationType;
		MemberName = memberName;
		SourceMemberType = sourceMemberType;
		DestinationMemberType = destinationMemberType;
		SourceValue = sourceValue;
	}

	public string? Stage { get; }
	public string? Path { get; }
	public Type? SourceType { get; }
	public Type? DestinationType { get; }
	public string? MemberName { get; }
	public Type? SourceMemberType { get; }
	public Type? DestinationMemberType { get; }
	public object? SourceValue { get; }
}
