using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GLV.Shared.Common;

namespace GLV.Shared.Data;

internal sealed class ErrorContainer
{
    public ErrorList ErrorList = new();
    public bool MarkedValid { get; set; }
}

public static class FormValidationExtensions
{
    private static ErrorList Fallback = new ErrorList(default, true);

    public static ref ErrorList GetErrorList(this FormValidationField? field) 
        => ref field?.Context is null ? ref Fallback : ref field.Value.Errors;
}

public class FormValidationContext(object model)
{
    public object Model { get; } = model ?? throw new ArgumentNullException(nameof(model));
    internal readonly Dictionary<PropertyInfo, ErrorContainer> errorsDict = new();
    private ErrorList Errors = new();

    public ref ErrorList GeneralErrors => ref Errors;

    public void AddError(ErrorMessage msg)
    {
        Errors.AddError(msg);
    }

    public ErrorList GetErrors()
    {
        var errors = new ErrorList();
        Errors.CopyTo(ref errors);

        foreach (var val in errorsDict.Values)
            val.ErrorList.CopyTo(ref errors);

        return errors;
    }

    public FormValidationField GetField(string name)
    {
        var prop = model.GetType().GetProperty(name.GetPropertyName());
        return prop is null
            ? throw new ArgumentException($"Model of type '{model.GetType().Name}' does not have a property called '{name}'")
            : new(prop, this);
    }

    public bool Invalid
        => Errors.Count > 0 || errorsDict.Values.Any(x => x.ErrorList.Count > 0);

    public void Clear()
    {
        errorsDict.Clear();
    }
}

public readonly struct FormValidationField
{
    public PropertyInfo? Property { get; }
    public FormValidationContext? Context { get; }

    internal FormValidationField(PropertyInfo? property, FormValidationContext? context)
    {
        Property = property;
        Context = context;
    }

    public readonly bool IsInvalid => Context is null || Context.errorsDict.TryGetValue(Property!, out var el) && el.ErrorList.Count > 0;

    public readonly string ValidationClass
        //=> Context?.errorsDict.TryGetValue(Property!, out var el) is true ? el.ErrorList.Count > 0 ? "modified invalid" : "modified valid" : "modified";
        => Context?.errorsDict.TryGetValue(Property!, out var el) is true 
            ? el.ErrorList.Count > 0 
                ? "modified invalid" 
                : el.MarkedValid 
                    ? "modified valid" 
                    : "modified" 
            : "";

    public readonly void AddError(ErrorMessage msg)
    {
        if (Context is null) return;
        Debug.Assert(Property is not null);
        if (Context.errorsDict.TryGetValue(Property, out var ec) is false)
        {
            ec = new();
            Context.errorsDict[Property] = ec;
        }

        ec.ErrorList.AddError(msg);
    }

    public readonly void MarkAsValid()
    {
        if (Context is null) return;
        Debug.Assert(Property is not null);
        if (Context.errorsDict.TryGetValue(Property, out var ec) is false)
        {
            ec = new();
            Context.errorsDict[Property] = ec;
        }

        ec.MarkedValid = true;
    }

    public static FormValidationField Empty() => new(null, null);

    public readonly ref ErrorList Errors
    {
        get
        {
            if (Context is null)
                throw new InvalidOperationException("Errors property cannot be accesed in an empty FormValidationField");

            Debug.Assert(Property is not null);
            if (Context.errorsDict.TryGetValue(Property, out var ec) is false)
            {
                ec = new();
                Context.errorsDict[Property] = ec;
            }

            return ref ec.ErrorList;
        }
    }
}
