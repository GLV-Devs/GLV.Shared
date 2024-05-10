namespace GLV.Shared.DataTransfer.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateDTOsAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NoRepositoryGenerationAttribute : Attribute { }