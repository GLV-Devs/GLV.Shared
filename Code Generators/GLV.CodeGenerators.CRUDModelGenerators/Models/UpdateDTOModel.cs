using System.Diagnostics;
using GLV.CodeGenerators.CRUDModelGenerators.Data;
using GLV.CodeGenerators.CRUDModelGenerators.Generators;
using GLV.Shared.Data;

namespace GLV.CodeGenerators.CRUDModelGenerators.Models;

public sealed class UpdateDTOModel(UpdateModelGenerator updateModelGenerator, Type originalType, string className, string? folder = null)
    : DTOModel(updateModelGenerator, originalType, className, folder)
{
    public override UpdateModelGenerator Generator { get; } = updateModelGenerator;

    public UpdateDTOModel(UpdateModelGenerator updateModelGenerator, Type originalType, string? folder = null)
        : this(updateModelGenerator, originalType, $"Update{originalType.Name}Model", folder) { }
    protected override ValueTask BeforeCommitToDisk(string directory, string path, CancellationToken ct = default)
    {
        modelBuilt = true;

        foreach (var data in properties)
        {
            Type t;
            string typename;
            switch (data.PropertyKind)
            {
                case PropertyKind.Id:
                    t = GeneratorHelpers.GetIdType(data.Type) ?? throw new InvalidDataException("Couldn't get the Id type of property kind Id");
                    typename = GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(Nullable.GetUnderlyingType(t) ?? t).Expression;
                    break;

                case PropertyKind.Flat:
                    t = data.Type;
                    typename = GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(Nullable.GetUnderlyingType(t) ?? t).Expression;
                    break;

                case PropertyKind.Collection:
                    var idtype = GeneratorHelpers.GetIdType(data.Type);
                    Debug.Assert(idtype is not null);
                    t = typeof(IEnumerable<>).MakeGenericType(typeof(EditAction<>).MakeGenericType(idtype));
                    typename = $"{GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(t).Expression}?";
                    break;

                default:
                    throw new InvalidDataException($"Unknown property kind: {data.PropertyKind}");
            }

            AddUsing(t.Namespace!);

            Contents.Append("\n\tpublic ");

            if (data.Property.PropertyType.IsValueType)
            {
                AddUsing("GLV.Shared.Data");
                Contents.Append("UpdateNullableStruct<");
                Contents.Append(typename);
                Contents.Append(">? ");
            }
            else
            {
                Contents.Append(typename);
                Contents.Append("? ");
            }

            Contents.Append(data.Property.Name);
            if (data.PropertyKind is PropertyKind.Id)
                Contents.Append("Id");
            Contents.Append(" { get; set; } ");
        }

        return ValueTask.CompletedTask;
    }
}
