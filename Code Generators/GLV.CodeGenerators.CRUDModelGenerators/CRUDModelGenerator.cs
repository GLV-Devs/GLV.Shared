using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using GLV.Shared.DataTransfer.Attributes;
using Serilog;
using Serilog.Events;

namespace GLV.CodeGenerators.CRUDModelGenerators;

public static class CRUDModelGenerator
{
    public static async Task Run(Assembly targetAssembly, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(targetAssembly);

        logger ??= new LoggerConfiguration()
            .WriteTo.Console(LogEventLevel.Verbose)
            .CreateLogger();

        GeneratorContext context = new([
            typeof(ViewGenerator),
            typeof(UpdateModelGenerator),
            typeof(CreateModelGenerator)
        ],
        "Output",
        "GVL.SistemaDeReportes.DataTransfer");

        int typeCount = 0;
        int eligibleTypeCount = 0;
        int viableTypeCount = 0;

        LoadReferencedAssembly(Assembly.GetExecutingAssembly());

        logger.Debug("Force loading all referenced assemblies");
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            LoadReferencedAssembly(assembly);

        logger.Information("Scanning Types in All Assemblies in the current domain");

        //AppDomain.CurrentDomain
        //        .GetAssemblies()
        //        .SelectMany(x => x.GetTypes().Where(x => x.IsClass && x.IsAbstract is false)

        foreach (var type in targetAssembly.GetTypes())
        {
            typeCount++;

            if (type.GetCustomAttribute<GenerateDTOs>() is null)
                continue;

            eligibleTypeCount++;
            bool isViable = false;

            logger.Information("Scanning {type}", type.FullName);
            foreach (var gen in context.Generators)
                isViable = gen.ScanType(type);

            if (isViable)
                viableTypeCount++;
        }

        logger.Information("Scanned {typeCount} types, found {eligibleTypeCount} eligible types for DTO generation and {viableTypeCount} types that contain valid annotations", typeCount, eligibleTypeCount, viableTypeCount);
        logger.Information("Generating DTOs and committing them to disk");
        await Task.WhenAll(context.Generators.Select(x => x.GenerateDTOs()));
        await context.RepositoryGenerator.GenerateDTOs();

        Dictionary<Type, List<string>> specialCases = [];
        foreach (var (type, model) in context.Generators.SelectMany(x => x.SpecialCases))
        {
            if (specialCases.TryGetValue(type, out var cases) is false)
                cases = specialCases[type] = [];
            cases.Add(model);
        }

        foreach (var (type, models) in specialCases)
            foreach (var str in models)
                logger.Warning("Found one or more special case properties in model {model} for type {type}", str, type);

        logger.Information("Generated DTOs for {viableTypeCount} types", viableTypeCount);
    }

    private static void LoadReferencedAssembly(Assembly assembly)
    {
        foreach (AssemblyName name in assembly.GetReferencedAssemblies())
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == name.FullName))
                LoadReferencedAssembly(Assembly.Load(name));
    }
}
