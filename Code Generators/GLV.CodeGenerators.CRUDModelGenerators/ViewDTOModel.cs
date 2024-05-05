using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public sealed class ViewDTOModel(ViewGenerator viewGenerator, Type originalType, string className, string? folder = null)
    : DTOModel(viewGenerator, originalType, className, folder)
{
    public override ViewGenerator Generator { get; } = viewGenerator;

    public ViewDTOModel(ViewGenerator viewGenerator, Type originalType, bool isEmbedded, string? folder = null)
        : this(viewGenerator, originalType, isEmbedded ? $"{originalType.Name}EmbeddedView" : $"{originalType.Name}View", folder) { }

    public void GetViewBuilderExpression(StringBuilder viewBuilder, int tabs = 3, string? parentProperty = null)
    {
        modelBuilt = true;
        foreach (var data in properties)
        {
            var originalType = data.Property.PropertyType;
            switch (data.PropertyKind)
            {
                case PropertyKind.Id:
                    viewBuilder.Append('\n')
                   .AddTabs(tabs)
                   .Append(data.Property.Name)
                   .Append("Id = model.")
                   .Append(parentProperty)
                   .Append(data.Property.Name)
                   .Append(".Id");
                    break;

                case PropertyKind.Flat:
                    viewBuilder.Append('\n')
                   .AddTabs(tabs)
                   .Append(data.Property.Name)
                   .Append(" = model.")
                   .Append(parentProperty)
                   .Append(data.Property.Name);
                    break;

                case PropertyKind.Embedded:
                    viewBuilder.Append('\n')
                   .AddTabs(tabs)
                   .Append(data.Property.Name)
                   .Append(" = model.")
                   .Append(parentProperty)
                   .Append(data.Property.Name)
                   .Append(" == null ? null : new()\n")
                   .AddTabs(tabs)
                   .Append('{');
                    Generator.GetEmbeddedViewFor(data.Property.PropertyType).GetViewBuilderExpression(viewBuilder, tabs + 1, $"{data.Property.Name}.");
                    viewBuilder.Append('\n')
                    .AddTabs(tabs)
                    .Append('}');
                    break;

                case PropertyKind.Collection:
                    viewBuilder.Append('\n')
                   .AddTabs(tabs)
                   .Append(data.Property.Name)
                   .Append(" = model.")
                   .Append(parentProperty)
                   .Append(data.Property.Name)
                   .Append(" == null ? null : model.")
                   .Append(parentProperty)
                   .Append(data.Property.Name)
                   .Append(".Select(x => x.Id)");
                    break;

                default:
                    throw new InvalidDataException($"Unknown property kind: {data.PropertyKind}");
            }

            viewBuilder.Append(',');
        }
    }
}
