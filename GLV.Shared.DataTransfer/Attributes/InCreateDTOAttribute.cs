namespace GLV.Shared.DataTransfer.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class InCreateDTOAttribute : Attribute
{
    public bool IsNullable { get; set; }
}

