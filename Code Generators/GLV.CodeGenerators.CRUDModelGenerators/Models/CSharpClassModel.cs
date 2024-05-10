using System.Text;

namespace GLV.CodeGenerators.CRUDModelGenerators.Models;

public abstract class CSharpClassModel(string className, string? folder, bool isStatic)
{
    protected readonly StringBuilder Contents = new(1000);
    private readonly HashSet<string> Namespaces = [];
    private readonly StringBuilder UsingStatements = new(100);

    public virtual string? Namespace { get; set; }

    public string ClassName { get; } = className;

    public string FileName { get; } = $"{className}.cs";

    public string? Folder { get; } = folder;

    public bool IsStatic { get; } = isStatic;

    public void AddUsing(string? @namespace)
    {
        if (@namespace is null) return;
        if (Namespaces.Add(@namespace))
            UsingStatements.Append("using ").Append(@namespace).Append(";\n");
    }

    protected virtual ValueTask BeforeCommitToDisk(string directory, string path, CancellationToken ct = default) => ValueTask.CompletedTask;

    protected virtual ValueTask AfterUsingStatements(StreamWriter output) => ValueTask.CompletedTask;

    protected virtual ValueTask AfterNamespace(StreamWriter output) => ValueTask.CompletedTask;

    protected virtual ValueTask AfterContents(StreamWriter output) => ValueTask.CompletedTask;

    protected virtual ValueTask AfterClassName(StreamWriter output) => ValueTask.CompletedTask;

    protected virtual ValueTask BeforeClassName(StreamWriter output) => ValueTask.CompletedTask;

    public virtual async Task CommitToDisk(string? basePath = null, CancellationToken ct = default)
    {
        var directory = Path.Combine(basePath ?? "", Folder ?? "");
        var path = Path.Combine(directory, FileName.EndsWith(".cs") ? FileName : $"{FileName}.cs");
        await BeforeCommitToDisk(directory, path, ct);

        Directory.CreateDirectory(directory);
        using var output = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.ReadWrite));

        await output.WriteAsync(UsingStatements, ct);

        await AfterUsingStatements(output);

        if (string.IsNullOrWhiteSpace(Namespace) is false)
        {
            output.Write("\nnamespace ");
            output.Write(Namespace);
        }

        await AfterNamespace(output);

        output.Write(";\n\n");

        await BeforeClassName(output);

        if (IsStatic)
            output.Write("public static class ");
        else
            output.Write("public sealed class ");

        output.Write(ClassName);

        await AfterClassName(output);

        output.Write("\n{");
        await output.WriteAsync(Contents, ct);

        await AfterContents(output);

        output.Write("\n}\n");
    }
}