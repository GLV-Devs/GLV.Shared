using System.Collections.Frozen;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public class GeneratorContext
{
    public RepositoryGenerator RepositoryGenerator { get; }
    public FrozenSet<IDTOGenerator> Generators { get; }

    public GeneratorContext(IEnumerable<Type> generators, string? basePath, string baseNamespace)
    {
        Generators = generators.Select(x =>
        {
            object?[] arguments = [basePath, baseNamespace, this];
            return x.IsAssignableTo(typeof(IDTOGenerator)) is false
                ? throw new ArgumentException($"Type {x} is not an IDTOGenerator")
                : (IDTOGenerator)Activator.CreateInstance(x, arguments)!;
        }).ToFrozenSet();

        RepositoryGenerator = new(basePath, baseNamespace, this);
    }
}
