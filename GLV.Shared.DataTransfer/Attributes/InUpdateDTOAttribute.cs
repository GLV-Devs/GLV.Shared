namespace GLV.Shared.DataTransfer.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class InUpdateDTOAttribute : Attribute
{
    public bool IsNullable { get; set; }
}

