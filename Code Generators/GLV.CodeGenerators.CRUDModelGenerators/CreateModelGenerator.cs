using GLV.Shared.DataTransfer.Attributes;
using Serilog;
using System.Collections;
using System.Reflection;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public class CreateModelGenerator(ILogger Log, string? basePath, string baseNamespace, GeneratorContext generatorContext) : IDTOGenerator
{
    public string? BasePath { get; } = basePath;
    public string Namespace { get; } = $"{baseNamespace ?? throw new ArgumentNullException(nameof(baseNamespace))}.CreateModels";

    public GeneratorContext GeneratorContext { get; } = generatorContext ?? throw new ArgumentNullException(nameof(generatorContext));

    private readonly Dictionary<Type, CreateDTOModel> CreateModels = [];
    private readonly Dictionary<Type, bool> ScannedCreateModels = [];

    public IEnumerable<(Type Type, string DTOClassName)> SpecialCases => specialCases;
    private readonly HashSet<(Type Type, string DTOClassName)> specialCases = [];

    public CreateDTOModel GetViewFor(Type type)
        => CreateModels[type];

    public bool ScanType(Type type)
    {
        if (ScannedCreateModels.TryGetValue(type, out var result))
        {
            Log.Verbose("{type} has already been scanned as a potential create model", type.FullName);
            return result;
        }

        Log.Debug("Scanning {type} to generate create model", type.FullName);
        CreateDTOModel? data = null;

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute<SpecialCaseDTOAttribute>() is not null)
                specialCases.Add((type, $"Create{type.Name}Model"));

            if (prop.GetCustomAttribute<InCreateDTOAttribute>() is null)
                continue;

            if (prop.PropertyType.IsAssignableTo(typeof(IEnumerable)) && prop.PropertyType != typeof(string))
            {
                var enumerableInterface = prop.PropertyType.GetInterfaces()
                                                           .Where(x => x.IsConstructedGenericType)
                                                           .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                          ?? throw new InvalidDataException("Collections that don't implement IEnumerble<> are not supported");

                var actualType = enumerableInterface.GetGenericArguments()[0];
                (data ??= new CreateDTOModel(this, type)).AddProperty(new()
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
                    (data ??= new CreateDTOModel(this, type)).AddProperty(new()
                    {
                        PropertyKind = PropertyKind.Flat,
                        Property = prop,
                        Type = GeneratorHelpers.CheckForAndReplaceAsString(prop.PropertyType),
                        IsNullable = nullable
                    });
                }
                else
                {
                    (data ??= new CreateDTOModel(this, type)).AddProperty(new()
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
            CreateModels.Add(type, data);
            GeneratorContext.RepositoryGenerator.GetModelFor(type).CreateModel = data;
            ScannedCreateModels.Add(type, true);
            return true;
        }

        ScannedCreateModels.Add(type, false);
        return false;
    }

    public Task GenerateDTOs()
        => Parallel.ForEachAsync(CreateModels, async (createmodel, ct) => await createmodel.Value.CommitToDisk(Path.Combine(BasePath ?? "", "CreateModels"), ct));
}
