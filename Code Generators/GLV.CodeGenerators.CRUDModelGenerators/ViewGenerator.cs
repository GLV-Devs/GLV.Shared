using System.Collections;
using System.Reflection;
using GLV.Shared.DataTransfer.Attributes;
using Serilog;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public class ViewGenerator(ILogger Log, string? basePath, string baseNamespace, GeneratorContext generatorContext) : IDTOGenerator
{
    private readonly Dictionary<Type, ViewDTOModel> Views = [];
    private readonly Dictionary<Type, ViewDTOModel> EmbeddedViews = [];
    private readonly Dictionary<Type, bool> ScannedEmbeddedViews = [];
    private readonly Dictionary<Type, bool> ScannedViews = [];

    public IEnumerable<(Type Type, string DTOClassName)> SpecialCases => specialCases;
    private readonly HashSet<(Type Type, string DTOClassName)> specialCases = [];

    public string? BasePath { get; } = basePath;

    public string Namespace { get; } = $"{baseNamespace ?? throw new ArgumentNullException(nameof(baseNamespace))}.UpdateModels";

    public GeneratorContext GeneratorContext { get; } = generatorContext ?? throw new ArgumentNullException(nameof(generatorContext));

    public ViewDTOModel GetEmbeddedViewFor(Type type)
        => EmbeddedViews[type];

    public ViewDTOModel GetViewFor(Type type)
        => Views[type];

    private bool ScanEmbeddedType(Type type)
    {
        if (ScannedEmbeddedViews.TryGetValue(type, out var result))
        {
            Log.Verbose("{type} has already been scanned as a potential embedded view", type.FullName);
            return result;
        }

        Log.Debug("Scanning {type} to generate embedded view", type.FullName);
        ViewDTOModel? data = null;

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute<InViewDTOAttribute>() is null)
                continue;

            if (prop.GetCustomAttribute<NotInEmbeddedView>() is not null)
                continue;

            if (prop.PropertyType.IsAssignableTo(typeof(IEnumerable)) && prop.PropertyType != typeof(string))
                continue;

            if (GeneratorHelpers.IsBasicType(prop.PropertyType, out var nullable))
            {
                (data ??= new ViewDTOModel(this, type, true)).AddProperty(new()
                {
                    PropertyKind = PropertyKind.Flat,
                    Property = prop,
                    Type = prop.PropertyType,
                    IsNullable = nullable
                });
            }
            else
            {
                var id = GeneratorHelpers.GetIdType(prop.PropertyType);
                if (id is null)
                    continue;

                (data ??= new ViewDTOModel(this, type, true)).AddProperty(new()
                {
                    PropertyKind = PropertyKind.Id,
                    Property = prop,
                    Type = id,
                });
            }
        }

        if (data is not null)
        {
            EmbeddedViews.Add(type, data);
            ScannedEmbeddedViews.Add(type, true);
            return true;
        }

        ScannedEmbeddedViews.Add(type, false);
        return false;
    }

    public bool ScanType(Type type)
    {
        if (ScannedViews.TryGetValue(type, out var result))
        {
            Log.Verbose("{type} has already been scanned as a potential view", type.FullName);
            return result;
        }

        Log.Debug("Scanning {type} to generate view", type.FullName);
        ViewDTOModel? data = null;

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute<SpecialCaseDTOAttribute>() is not null)
                specialCases.Add((type, $"{type.Name}View"));

            if (prop.GetCustomAttribute<InViewDTOAttribute>() is null)
                continue;

            if (prop.PropertyType.IsAssignableTo(typeof(IEnumerable)) && prop.PropertyType != typeof(string))
            {
                var enumerableInterface = prop.PropertyType.GetInterfaces()
                                                           .Where(x => x.IsConstructedGenericType)
                                                           .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                          ?? throw new InvalidDataException("Collections that don't implement IEnumerble<> are not supported");

                var actualType = enumerableInterface.GetGenericArguments()[0];
                (data ??= new ViewDTOModel(this, type, false)).AddProperty(new()
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
                    (data ??= new ViewDTOModel(this, type, false)).AddProperty(new()
                    {
                        PropertyKind = PropertyKind.Flat,
                        Property = prop,
                        Type = prop.PropertyType,
                        IsNullable = nullable
                    });
                }
                else if (ScanEmbeddedType(prop.PropertyType))
                {
                    (data ??= new ViewDTOModel(this, type, false)).AddProperty(new()
                    {
                        PropertyKind = PropertyKind.Embedded,
                        Property = prop,
                        Type = prop.PropertyType
                    });
                }
                else
                {
                    (data ??= new ViewDTOModel(this, type, false)).AddProperty(new()
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
            Views.Add(type, data);
            GeneratorContext.RepositoryGenerator.GetModelFor(type).ViewModel = data;
            ScannedViews.Add(type, true);
            return true;
        }

        ScannedViews.Add(type, false);
        return false;
    }

    public Task GenerateDTOs()
        => Task.WhenAll(
            Parallel.ForEachAsync(Views, async (view, ct) => await view.Value.CommitToDisk(Path.Combine(BasePath ?? "", "Views"), ct)),
            Parallel.ForEachAsync(EmbeddedViews, async (embedded, ct) => await embedded.Value.CommitToDisk(Path.Combine(BasePath ?? "", "Views"), ct))
        );
}
