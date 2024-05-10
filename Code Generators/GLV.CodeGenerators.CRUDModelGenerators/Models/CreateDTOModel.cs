using GLV.CodeGenerators.CRUDModelGenerators.Generators;

namespace GLV.CodeGenerators.CRUDModelGenerators.Models;

public sealed class CreateDTOModel(CreateModelGenerator updateModelGenerator, Type originalType, string className, string? folder = null)
    : DTOModel(updateModelGenerator, originalType, className, folder)
{
    public override CreateModelGenerator Generator { get; } = updateModelGenerator;

    public CreateDTOModel(CreateModelGenerator updateModelGenerator, Type originalType, string? folder = null)
        : this(updateModelGenerator, originalType, $"Create{originalType.Name}Model", folder) { }
}
