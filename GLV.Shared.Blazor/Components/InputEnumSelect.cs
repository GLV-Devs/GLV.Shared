﻿/*

Original code sourced from:
Repo: https://github.com/meziantou/Meziantou.Framework/
Commited file: https://github.com/meziantou/Meziantou.Framework/blob/ee664b6cf25ab0ae70ceaee55fcd3ef77c30dc4d/src/Meziantou.AspNetCore.Components/InputEnumSelect.cs

Under MIT License

 */

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Blazor.Components;

public class InputEnumSelect<TEnum> : InputBase<TEnum> where TEnum : unmanaged, Enum
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "select");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass);
        builder.AddAttribute(3, "value", BindConverter.FormatValue(CurrentValueAsString));
        builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string>(this, value => CurrentValueAsString = value, CurrentValueAsString ?? "", culture: null));

        // Add an option element per enum value
        var enumType = InputEnumSelect<TEnum>.GetEnumType();
        foreach (TEnum value in Enum.GetValues(enumType))
        {
            builder.OpenElement(5, "option");
            builder.AddAttribute(6, "value", value.ToString());
            builder.AddContent(7, GetDisplayName(value));
            builder.CloseElement();
        }

        builder.CloseElement(); // close the select element
    }

    protected override bool TryParseValueFromString(string? value, out TEnum result, out string validationErrorMessage)
    {
        // Let's Blazor convert the value for us 😊
        if (BindConverter.TryConvertTo(value, CultureInfo.CurrentCulture, out TEnum parsedValue))
        {
            result = parsedValue;
            validationErrorMessage = "";
            return true;
        }

        // Map null/empty value to null if the bound object is nullable
        if (string.IsNullOrEmpty(value))
        {
            var nullableType = Nullable.GetUnderlyingType(typeof(TEnum));
            if (nullableType != null)
            {
                result = default;
                validationErrorMessage = "";
                return true;
            }
        }

        // The value is invalid => set the error message
        result = default;
        validationErrorMessage = $"The {FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    // Get the display text for an enum value:
    // - Use the DisplayAttribute if set on the enum member, so this support localization
    // - Fallback on Humanizer to decamelize the enum member name
    private static string GetDisplayName(TEnum value)
    {
        // Read the Display attribute name
        var member = value.GetType().GetMember(value.ToString())[0];
        var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.GetName() ?? value.ToString();
    }

    // Get the actual enum type. It unwrap Nullable<T> if needed
    // MyEnum  => MyEnum
    // MyEnum? => MyEnum
    private static Type GetEnumType()
    {
        var nullableType = Nullable.GetUnderlyingType(typeof(TEnum));
        if (nullableType != null)
            return nullableType;

        return typeof(TEnum);
    }
}
