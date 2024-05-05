using GLV.Shared.DataTransfer.Attributes;
using Serilog;
using System.Collections;
using System.Reflection;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public class UpdateModelGenerator(ILogger Log, string? basePath, string baseNamespace, GeneratorContext generatorContext) : IDTOGenerator
{
    public string? BasePath { get; } = basePath;
    public string Namespace { get; } = $"{baseNamespace ?? throw new ArgumentNullException(nameof(baseNamespace))}.UpdateModels";

    public GeneratorContext GeneratorContext { get; } = generatorContext ?? throw new ArgumentNullException(nameof(generatorContext));

    public IEnumerable<(Type Type, string DTOClassName)> SpecialCases => specialCases;
    private readonly HashSet<(Type Type, string DTOClassName)> specialCases = [];

    private readonly Dictionary<Type, UpdateDTOModel> UpdateModels = [];
    private readonly Dictionary<Type, bool> ScannedUpdateModels = [];

    public UpdateDTOModel GetViewFor(Type type)
        => UpdateModels[type];

    public bool ScanType(Type type)
    {
        if (ScannedUpdateModels.TryGetValue(type, out var result))
        {
            Log.Verbose("{type} has already been scanned as a potential update model", type.FullName);
            return result;
        }

        Log.Debug("Scanning {type} to generate update model", type.FullName);
        UpdateDTOModel? data = null;

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute<SpecialCaseDTOAttribute>() is not null)
                specialCases.Add((type, $"Update{type.Name}Model"));

            if (prop.GetCustomAttribute<InUpdateDTOAttribute>() is null)
                continue;

            if (prop.PropertyType.IsAssignableTo(typeof(IEnumerable)) && prop.PropertyType != typeof(string))
            {
                var enumerableInterface = prop.PropertyType.GetInterfaces()
                                                           .Where(x => x.IsConstructedGenericType)
                                                           .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                          ?? throw new InvalidDataException("Collections that don't implement IEnumerble<> are not supported");

                var actualType = enumerableInterface.GetGenericArguments()[0];
                (data ??= new UpdateDTOModel(this, type)).AddProperty(new()
                {
                    PropertyKind = PropertyKind.Collection,
                    Property = prop,
                    Type = actualType
                });
            }
            else
            {
                if (GeneratorHelpers.IsBasicType(prop.PropertyType, out var nullable))
                {
                    (data ??= new UpdateDTOModel(this, type)).AddProperty(new()
                    {
                        PropertyKind = PropertyKind.Flat,
                        Property = prop,
                        Type = GeneratorHelpers.CheckForAndReplaceAsString(prop.PropertyType),
                        IsNullable = nullable
                    });
                }
                else
                {
                    (data ??= new UpdateDTOModel(this, type)).AddProperty(new()
                    {
                        PropertyKind = PropertyKind.Id,
                        Property = prop,
                        Type = prop.PropertyType
                    });
                }
            }
        }

        if (data is not null)
        {
            UpdateModels.Add(type, data);
            GeneratorContext.RepositoryGenerator.GetModelFor(type).UpdateModel = data;
            ScannedUpdateModels.Add(type, true);
            return true;
        }

        ScannedUpdateModels.Add(type, false);
        return false;
    }

    public Task GenerateDTOs()
        => Parallel.ForEachAsync(UpdateModels, async (updatemodel, ct) => await updatemodel.Value.CommitToDisk(Path.Combine(BasePath ?? "", "UpdateModels"), ct));
}
