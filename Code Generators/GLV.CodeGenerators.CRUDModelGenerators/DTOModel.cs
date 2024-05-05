using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public abstract class DTOModel(IDTOGenerator generator, Type originalType, string className, string? folder = null)
    : CSharpClassModel(className, folder, false)
{
    public Type OriginalType { get; } = originalType;

    private readonly HashSet<Type> Interfaces = [];
    protected readonly HashSet<PropertyData> properties = [];

    public IEnumerable<PropertyData> Properties => properties;

    protected bool modelBuilt;

    public virtual IDTOGenerator Generator { get; } = generator;

    public override string? Namespace
    {
        get => Generator.Namespace; set { }
    }

    protected override ValueTask AfterClassName(StreamWriter output)
    {
        if (Interfaces.Count > 0)
        {
            output.Write(" : ");

            Type t;
            var enume = Interfaces.GetEnumerator();

            if (enume.MoveNext())
            {
                t = enume.Current;
                output.Write(GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(t));
            }

            while (enume.MoveNext())
            {
                t = enume.Current;
                output.Write(", ");
                output.Write(GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(t));
            }
        }

        return ValueTask.CompletedTask;
    }

    public void AddImplementedInterface(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsInterface is false)
            throw new InvalidOperationException("Can only add interfaces");

        if (type.IsGenericType && type.IsConstructedGenericType is false)
            throw new InvalidOperationException("A class cannot implement an open generic interface");

        if (Interfaces.Add(type))
            AddUsing(type.Namespace);
    }

    public void AddProperty(PropertyData data)
    {
        if (modelBuilt)
            throw new InvalidOperationException("Cannot add new properties after the View Builder Expression has been generated");

        properties.Add(data);
    }

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
                    typename = GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(t).Expression;
                    break;

                case PropertyKind.Flat:
                    t = data.Type;
                    typename = GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(t).Expression;
                    break;

                case PropertyKind.Embedded:
                    t = data.Type;
                    typename = $"{t.Name}EmbeddedView?";
                    break;

                case PropertyKind.Collection:
                    var idtype = GeneratorHelpers.GetIdType(data.Type);
                    Debug.Assert(idtype is not null);
                    t = typeof(IEnumerable<>).MakeGenericType(idtype);
                    typename = $"{GeneratorHelpers.BuildTypeNameAsCSharpTypeExpression(t).Expression}?";
                    break;

                default:
                    throw new InvalidDataException($"Unknown property kind: {data.PropertyKind}");
            }

            AddUsing(t.Namespace!);

            Contents.Append("\n\tpublic ");
            Contents.Append(typename);
            Contents.Append(' ');
            Contents.Append(data.Property.Name);
            if (data.PropertyKind is PropertyKind.Id)
                Contents.Append("Id");
            Contents.Append(" { get; set; } ");
        }

        return ValueTask.CompletedTask;
    }
}
