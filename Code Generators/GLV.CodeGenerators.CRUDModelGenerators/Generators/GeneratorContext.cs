using System.Collections.Frozen;
using Serilog;

namespace GLV.CodeGenerators.CRUDModelGenerators.Generators;

public class GeneratorContext
{
    public RepositoryGenerator RepositoryGenerator { get; }
    public FrozenSet<IDTOGenerator> Generators { get; }

    public GeneratorContext(ILogger log, IEnumerable<Type> generators, string? basePath, string baseNamespace)
    {
        Generators = generators.Select(x =>
        {
            object?[] arguments = [log, basePath, baseNamespace, this];
            return x.IsAssignableTo(typeof(IDTOGenerator)) is false
                ? throw new ArgumentException($"Type {x} is not an IDTOGenerator")
                : (IDTOGenerator)Activator.CreateInstance(x, arguments)!;
        }).ToFrozenSet();

        RepositoryGenerator = new(basePath, baseNamespace, this);
    }
}
