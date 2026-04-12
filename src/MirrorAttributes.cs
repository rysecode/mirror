namespace Mirror;

[AttributeUsage(AttributeTargets.Property)]
public class MirrorDeepAttribute : Attribute
{
	public bool MapChildren { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class MirrorNonReflectAttribute : Attribute
{
}
