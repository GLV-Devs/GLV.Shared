using System.Collections.Concurrent;
using System.Reflection;
using GLV.CodeGenerators.CRUDModelGenerators.Models;
using GLV.Shared.DataTransfer.Attributes;

namespace GLV.CodeGenerators.CRUDModelGenerators.Generators;

public class RepositoryGenerator(string? basePath, string @namespace, GeneratorContext generatorContext)
{
    private readonly ConcurrentDictionary<Type, RepositoryDTOModel> RepositoryModels = [];

    public RepositoryDTOModel? GetModelFor(Type type) 
        => type.GetCustomAttribute<NoRepositoryGenerationAttribute>() != null
            ? null
            : RepositoryModels.GetOrAdd(type, t => new RepositoryDTOModel(this, t, null));

    public string? BasePath { get; } = basePath;
    public string Namespace { get; } = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
    public GeneratorContext GeneratorContext { get; } = generatorContext ?? throw new ArgumentNullException(nameof(generatorContext));

    public Task GenerateDTOs()
        => Parallel.ForEachAsync(RepositoryModels, async (view, ct) => await view.Value.CommitToDisk(Path.Combine(BasePath ?? "", "Repositories"), ct));
}
