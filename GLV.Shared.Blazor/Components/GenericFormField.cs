﻿/*

Original code sourced from:
Repo: https://github.com/meziantou/Meziantou.Framework/
Commited file: https://github.com/meziantou/Meziantou.Framework/blob/ee664b6cf25ab0ae70ceaee55fcd3ef77c30dc4d/src/Meziantou.AspNetCore.Components/GenericFormField.cs

Under MIT License

 */

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GLV.Shared.Blazor.Components;

public sealed class GenericFormField<TModel>
{
    private static readonly MethodInfo s_eventCallbackFactoryCreate = GetEventCallbackFactoryCreate();

    private readonly GenericForm<TModel> _form;
    private RenderFragment? _editorTemplate;
    private RenderFragment? _fieldValidationTemplate;

    public event EventHandler? ValueChanged;

    private GenericFormField(GenericForm<TModel> form, PropertyInfo propertyInfo)
    {
        _form = form;
        Property = propertyInfo;
    }

    internal static List<GenericFormField<TModel>> Create(GenericForm<TModel> form)
    {
        var result = new List<GenericFormField<TModel>>();
        var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        foreach (var prop in properties)
        {
            // Skip readonly properties
            if (prop.SetMethod == null)
                continue;

            if (prop.GetCustomAttribute<EditableAttribute>() is { } editor && !editor.AllowEdit)
                continue;

            var field = new GenericFormField<TModel>(form, prop);
            result.Add(field);
        }

        return result;
    }

    public PropertyInfo Property { get; }
    public string EditorId => _form.BaseEditorId + '_' + Property.Name;
    public TModel Owner => _form.Model!;

    public string DisplayName
    {
        get
        {
            var displayAttribute = Property.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
            {
                var displayName = displayAttribute.GetName();
                if (!string.IsNullOrEmpty(displayName))
                    return displayName;
            }

            var displayNameAttribute = Property.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute != null)
            {
                var displayName = displayNameAttribute.DisplayName;
                if (!string.IsNullOrEmpty(displayName))
                    return displayName;
            }

            return Property.Name;
        }
    }

    public string? Description
    {
        get
        {
            var displayAttribute = Property.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
            {
                var description = displayAttribute.GetDescription();
                if (!string.IsNullOrEmpty(description))
                    return description;
            }

            var descriptionAttribute = Property.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                var description = descriptionAttribute.Description;
                if (!string.IsNullOrEmpty(description))
                    return description;
            }

            return null;
        }
    }

    public Type PropertyType => Property.PropertyType;

    public object? Value
    {
        get => Property.GetValue(Owner);
        set
        {
            if (Property.SetMethod != null && !Equals(Value, value))
            {
                Property.SetValue(Owner, value);
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public RenderFragment EditorTemplate
    {
        get
        {
            if (_editorTemplate != null)
                return _editorTemplate;

            // () => Owner.Property
            var access = Expression.Property(Expression.Constant(Owner, typeof(TModel)), Property);
            var lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(PropertyType), access);

            // Create(object receiver, Action<object> callback
            var method = s_eventCallbackFactoryCreate.MakeGenericMethod(PropertyType);

            // value => Field.Value = value;
            var changeHandlerParameter = Expression.Parameter(PropertyType);
            var body = Expression.Assign(Expression.Property(Expression.Constant(this), nameof(Value)), Expression.Convert(changeHandlerParameter, typeof(object)));
            var changeHandlerLambda = Expression.Lambda(typeof(Action<>).MakeGenericType(PropertyType), body, changeHandlerParameter);
            var changeHandler = method.Invoke(EventCallback.Factory, new object[] { this, changeHandlerLambda.Compile() });

            return _editorTemplate ??= builder =>
            {
                var (componentType, additonalAttributes) = GetEditorType(Property);
                builder.OpenComponent(0, componentType);
                builder.AddAttribute(1, "Value", Value);
                builder.AddAttribute(2, "ValueChanged", changeHandler);
                builder.AddAttribute(3, "ValueExpression", lambda);
                builder.AddAttribute(4, "id", EditorId);
                builder.AddAttribute(5, "class", _form.FormValidationContext is null ? _form.EditorClass : $"{_form.EditorClass} {_form.FormValidationContext.GetField(Property.Name).ValidationClass}");
                builder.AddMultipleAttributes(6, additonalAttributes);
                builder.CloseComponent();
            };
        }
    }

    public RenderFragment? FieldValidationTemplate
    {
        get
        {
            if (!_form.EnableFieldValidation)
                return null;

            return _fieldValidationTemplate ??= builder =>
            {
                // () => Owner.Property
                var access = Expression.Property(Expression.Constant(Owner, typeof(TModel)), Property);
                var lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(PropertyType), access);

                builder.OpenComponent(0, typeof(ValidationMessage<>).MakeGenericType(PropertyType));
                builder.AddAttribute(1, "For", lambda);
                builder.CloseComponent();
            };
        }
    }

    [SuppressMessage("Style", "MA0003:Add argument name to improve readability", Justification = "Not needed")]
    private static (Type ComponentType, IEnumerable<KeyValuePair<string, object>>? AdditonalAttributes) GetEditorType(PropertyInfo property)
    {
        var editorAttributes = property.GetCustomAttributes<EditorAttribute>();
        foreach (var editorAttribute in editorAttributes)
        {
            if (editorAttribute.EditorBaseTypeName == typeof(InputBase<>).AssemblyQualifiedName)
                return (Type.GetType(editorAttribute.EditorTypeName, throwOnError: true)!, null);
        }

        if (property.PropertyType == typeof(bool))
            return (typeof(InputCheckbox), null);

        if (property.PropertyType == typeof(string))
        {
            var dataType = property.GetCustomAttribute<DataTypeAttribute>();
            if (dataType != null)
            {
                if (dataType.DataType == DataType.Date)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "date") });

                if (dataType.DataType == DataType.DateTime)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "datetime-local") });

                if (dataType.DataType == DataType.EmailAddress)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "email") });

                if (dataType.DataType == DataType.MultilineText)
                    return (typeof(InputTextArea), null);

                if (dataType.DataType == DataType.Password)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "password") });

                if (dataType.DataType == DataType.PhoneNumber)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "tel") });

                if (dataType.DataType == DataType.Time)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "time") });

                if (dataType.DataType == DataType.Url)
                    return (typeof(InputText), new[] { KeyValuePair.Create<string, object>("type", "url") });
            }

            return (typeof(InputText), null);
        }

        if (property.PropertyType == typeof(short))
            return (typeof(InputNumber<short>), null);

        if (property.PropertyType == typeof(int))
            return (typeof(InputNumber<int>), null);

        if (property.PropertyType == typeof(long))
            return (typeof(InputNumber<long>), null);

        if (property.PropertyType == typeof(float))
            return (typeof(InputNumber<float>), null);

        if (property.PropertyType == typeof(double))
            return (typeof(InputNumber<double>), null);

        if (property.PropertyType == typeof(decimal))
            return (typeof(InputNumber<decimal>), null);

        if (property.PropertyType == typeof(DateTime))
        {
            var dataType = property.GetCustomAttribute<DataTypeAttribute>();
            if (dataType != null && dataType.DataType == DataType.Date)
                return (typeof(InputDate<DateTime>), null);

            return (typeof(InputDateTime<DateTime>), null);
        }

        if (property.PropertyType == typeof(DateTimeOffset))
        {
            var dataType = property.GetCustomAttribute<DataTypeAttribute>();
            if (dataType != null && dataType.DataType == DataType.Date)
                return (typeof(InputDate<DateTimeOffset>), null);

            return (typeof(InputDateTime<DateTimeOffset>), null);
        }

        if (property.PropertyType.IsEnum)
        {
            if (!property.PropertyType.IsDefined(typeof(FlagsAttribute), inherit: true))
                return (typeof(InputEnumSelect<>).MakeGenericType(property.PropertyType), null);
        }

        return (typeof(InputText), null);
    }

    private static MethodInfo GetEventCallbackFactoryCreate()
    {
        return typeof(EventCallbackFactory).GetMethods()
            .Single(m =>
            {
                if (m.Name != "Create" || !m.IsPublic || m.IsStatic || !m.IsGenericMethod)
                    return false;

                var generic = m.GetGenericArguments();
                if (generic.Length != 1)
                    return false;

                var args = m.GetParameters();
                return args.Length == 2 && args[0].ParameterType == typeof(object) && args[1].ParameterType.IsGenericType && args[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>);
            });
    }
}
