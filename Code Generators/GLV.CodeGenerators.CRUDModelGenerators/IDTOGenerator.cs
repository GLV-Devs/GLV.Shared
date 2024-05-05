using System.Collections.Concurrent;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public interface IDTOGenerator
{
    public string? BasePath { get; }
    public string Namespace { get; }

    public GeneratorContext GeneratorContext { get; }

    public IEnumerable<(Type Type, string DTOClassName)> SpecialCases { get; }

    public bool ScanType(Type type);
    public Task GenerateDTOs();
}
