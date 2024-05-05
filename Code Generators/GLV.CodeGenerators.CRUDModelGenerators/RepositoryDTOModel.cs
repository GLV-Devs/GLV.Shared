using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public class RepositoryDTOModel(RepositoryGenerator generator, Type modelType, string? folder)
    : CSharpClassModel($"{modelType.Name}Repository", folder, false)
{
    public override string? Namespace
    {
        get => Generator.Namespace;
        set { }
    }

    public Type ModelType { get; } = modelType;

    public RepositoryGenerator Generator { get; } = generator;

    [DisallowNull]
    public ViewDTOModel? ViewModel { get; set; }

    [DisallowNull]
    public UpdateDTOModel? UpdateModel { get; set; }

    [DisallowNull]
    public CreateDTOModel? CreateModel { get; set; }

    protected override ValueTask BeforeClassName(StreamWriter output)
    {
        Debug.Assert(ViewModel is not null);
        Debug.Assert(CreateModel is not null);
        Debug.Assert(UpdateModel is not null);

        output.Write("[RegisterService(typeof(IRepository<");
        output.Write(ModelType.Name);
        output.Write(", ");
        output.Write(GeneratorHelpers.GetIdType(ModelType));
        output.Write(", ");
        output.Write(ViewModel.ClassName);
        output.Write(", ");
        output.Write(CreateModel.ClassName);
        output.Write(", ");
        output.Write(UpdateModel.ClassName);
        output.Write(">))]\n");

        output.Write("[RegisterService(typeof(");
        output.Write(ClassName);
        output.Write("))]\n");

        return ValueTask.CompletedTask;
    }

    protected override ValueTask AfterClassName(StreamWriter output)
    {
        Debug.Assert(ViewModel is not null);
        Debug.Assert(CreateModel is not null);
        Debug.Assert(UpdateModel is not null);

        output.Write("(ReportesContext context) : EntityFrameworkRepository<");
        output.Write(ModelType.Name);
        output.Write(", ");
        output.Write(GeneratorHelpers.GetIdType(ModelType));
        output.Write(", ");
        output.Write(ViewModel.ClassName);
        output.Write(", ");
        output.Write(CreateModel.ClassName);
        output.Write(", ");
        output.Write(UpdateModel.ClassName);
        output.Write(", ReportesContext>(context)");

        return ValueTask.CompletedTask;
    }

    protected override ValueTask BeforeCommitToDisk(string directory, string path, CancellationToken ct = default)
    {
        if (ViewModel is null)
            throw new InvalidOperationException($"All DTO models must be set before the repository can be generated. ViewModel is missing for type: {ModelType.Name}");

        if (UpdateModel is null)
            throw new InvalidOperationException($"All DTO models must be set before the repository can be generated. UpdateModel is missing for type: {ModelType.Name}");

        if (CreateModel is null)
            throw new InvalidOperationException($"All DTO models must be set before the repository can be generated. CreateModel is missing for type: {ModelType.Name}");

        AddUsing(ViewModel.Namespace);
        AddUsing("System");
        AddUsing("System.Linq");
        AddUsing("System.Linq.Expressions");
        AddUsing("GVL.SistemaDeReportes.Servidor.DataTransfer");
        AddUsing("GVL.SistemaDeReportes.Shared.Server");
        AddUsing("GVL.SistemaDeReportes.Shared.Common");
        AddUsing("GVL.SistemaDeReportes.Servidor.Database");
        AddUsing("GVL.SistemaDeReportes.Servidor.Data.Repositories.Base");
        AddUsing("GVL.SistemaDeReportes.DataTransfer.CreateModels");
        AddUsing("Microsoft.EntityFrameworkCore");

        Contents.Append("\n\n\tpublic override ");
        Contents.Append(ViewModel.ClassName);
        Contents.Append(" GetView( ");
        Contents.Append(ModelType.Name);
        Contents.Append(" model)\n\t\t=> new()\n\t\t{");
        ViewModel.GetViewBuilderExpression(Contents);
        Contents.Append("\n\t\t};");

        Contents.Append("\n\n\tprotected override Expression<Func<");
        Contents.Append(ModelType.Name);
        Contents.Append(", ");
        Contents.Append(ViewModel.ClassName);
        Contents.Append(">> ");
        Contents.Append("ViewExpression { get; } = model => new()\n\t\t{");
        ViewModel.GetViewBuilderExpression(Contents);
        Contents.Append("\n\t\t};");

        // ------------------------ Create Method

        Contents.Append("\n\n\tpublic override async ValueTask<SuccessResult<");
        Contents.Append(ModelType.Name);
        Contents.Append(">> Create(");
        Contents.Append(CreateModel.ClassName);
        Contents.Append(" creationModel)\n\t{\n\t\tErrorList errors = new();\n");

        foreach (var prop in CreateModel.Properties)
        {
            if (prop.Type == typeof(string))
            {
                Contents.Append("\n\n\t\tModelManipulationHelper.IsEmptyString(ref errors, creationModel.");
                Contents.Append(prop.Property.Name);
                Contents.Append(");");

                var originalType = prop.Property.PropertyType;

                if (GeneratorHelpers.CanBeReplacedWithString(originalType))
                {
                    AddUsing(originalType.Namespace);

                    Contents.Append("\n\t\tif (");
                    Contents.Append(originalType.Name);
                    Contents.Append(".TryParse(creationModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(", out var parsed");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(") is false)");
                    Contents.Append("\n\t\t\terrors.AddInvalidProperty(nameof(creationModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append("));");
                }
            }
            else if (prop.Property.Name.EndsWith("Id") && FetchOriginalProp(prop, prop.Property.Name, out var originalProperty))
            {
                Contents.Append("\n\n\t\tif (await Context.Set<");
                Contents.Append(originalProperty.PropertyType.Name!);
                Contents.Append(">().AnyAsync(x => x.Id == creationModel.");
                Contents.Append(prop.Property.Name);
                Contents.Append(") is false)\n\t\t\terrors.AddEntityNotFound(nameof(");
                Contents.Append(originalProperty.PropertyType.Name!);
                Contents.Append("), $\"id:{creationModel.");
                Contents.Append(prop.Property.Name);
                Contents.Append("}\");");
            }
            else
            {
                Contents.Append("\n#error Handle property: creationModel.");
                Contents.Append(prop.Property.Name);
            }
        }

        Contents.Append("\n\n\t\tif (errors.Count > 0)\n\t\t\t\treturn errors;");
        Contents.Append("\n\n\t\tvar ent = new ");
        Contents.Append(ModelType.Name);
        Contents.Append("()\n\t\t{");

        foreach (var prop in CreateModel.Properties)
        {
            var originalType = prop.Property.PropertyType;

            if (GeneratorHelpers.CanBeReplacedWithString(originalType))
            {
                Contents.Append("\n\t\t\t");
                Contents.Append(prop.Property.Name);
                Contents.Append(" = parsed");
                Contents.Append(prop.Property.Name);
                Contents.Append(',');
            }
            else
            {
                Contents.Append("\n\t\t\t");
                Contents.Append(prop.Property.Name);
                Contents.Append(" = creationModel.");
                Contents.Append(prop.Property.Name);
                Contents.Append(',');
            }
        }

        Contents.Append("\n\t\t};\n\n\t\tContext.Set<");
        Contents.Append(ModelType.Name);
        Contents.Append(">().Add(ent);");
        Contents.Append("\n\t\treturn ent;\n\t}");

        // ------------------------ Update Method

        Contents.Append("\n\n\tpublic override async ValueTask<SuccessResult<");
        Contents.Append(ViewModel.ClassName);
        Contents.Append(">?> Update(");
        Contents.Append(GeneratorHelpers.GetIdType(ModelType)!.Name);
        Contents.Append(" key, ");
        Contents.Append(UpdateModel.ClassName);
        Contents.Append(" updateModel)\n\t{\n\t\tErrorList errors = new();\n\n\t\tvar ent = await Context.Set<");
        Contents.Append(ModelType.Name);
        Contents.Append(">().FirstOrDefaultAsync(x => x.Id.Equals(key));\n\t\tif (ent is null)\n\t\t\treturn errors.AddEntityNotFound(nameof(");
        Contents.Append(ModelType.Name);
        Contents.Append("), $\"id:{key}\");\n");

        foreach (var prop in UpdateModel.Properties)
        {
            var originalType = prop.Property.PropertyType;

            if (prop.Type == typeof(string))
            {
                if (GeneratorHelpers.CanBeReplacedWithString(originalType))
                {
                    AddUsing(originalType.Namespace);

                    Contents.Append("\n\t\tif (ModelManipulationHelper.IsUpdatingNullableString(updateModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(", out var update");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(") is false || ");
                    Contents.Append(originalType.Name);
                    Contents.Append(".TryParse(update");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(", out var parsed");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(") is false)");
                    Contents.Append("\n\t\t\terrors.AddInvalidProperty(nameof(updateModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append("));\n\t\telse\n\t\t\tent.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(" = parsed");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(";\n");
                }
                else
                {
                    Contents.Append("\n\n\t\tif (ModelManipulationHelper.IsUpdating(ent.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(", updateModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append("))");

                    Contents.Append("\n\t\t\tent.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(" = updateModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(";\n");
                }
            }
            else if (prop.Type.IsValueType)
            {
                Contents.Append("\n\t\tif (ModelManipulationHelper.IsUpdatingNullable(updateModel.");
                Contents.Append(prop.Property.Name);
                Contents.Append(", out var update");
                Contents.Append(prop.Property.Name);
                Contents.Append("))");

                if (Nullable.GetUnderlyingType(prop.Property.PropertyType) is null)
                {
                    Contents.Append("\n\t\t\tif (update");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(" is not ");
                    Contents.Append(prop.Property.PropertyType.Name);
                    Contents.Append(" nullChecked");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(")\n\t\t\t\terrors.AddEmptyProperty(nameof(updateModel.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append("));\n\t\t\telse\n\t\t\t\tent.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(" = nullChecked");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(";\n");
                }
                else
                {
                    Contents.Append("\n\t\t\tent.");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(" = update");
                    Contents.Append(prop.Property.Name);
                    Contents.Append(";\n");
                }
            }
            else
            {
                Contents.Append("\n#error Handle property: updateModel.");
                Contents.Append(prop.Property.Name);
            }
        }

        Contents.Append("\n\t\treturn errors.Count > 0 ? errors : GetView(ent);\n\t}");

        return ValueTask.CompletedTask;
    }

    private static bool FetchOriginalProp(PropertyData prop, string name, [NotNullWhen(true)] out PropertyInfo? originalProperty)
    {
        Debug.Assert(name.EndsWith("Id"));
        Debug.Assert(name.Length > 2);

        originalProperty = prop.Property.DeclaringType!.GetProperty(name[..^2]);
        return originalProperty is not null;
    }
}
