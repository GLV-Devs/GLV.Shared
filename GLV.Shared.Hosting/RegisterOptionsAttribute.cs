namespace GLV.Shared.Hosting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RegisterOptionsAttribute : Attribute
{
    public string? SectionName { get; init; }
    public string? OptionsName { get; init; }
    public bool Monitor { get; init; }
}
