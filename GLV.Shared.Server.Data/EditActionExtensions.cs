using GLV.Shared.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Server.Data;

public static class EditActionExtensions
{
    public static void PerformActionsString(this IEnumerable<EditAction<string>> actions, ICollection<string> values, ref ErrorList errors, EditActionCheck<string>? check = null, [CallerArgumentExpression(nameof(actions))] string propertyName = "")
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(actions);
        bool active = true;

        int index = 0;
        foreach (var editAction in actions)
        {
            if (check?.Invoke(editAction, index) is ErrorMessage msg)
            {
                errors.AddError(msg);
                active = false;
                continue;
            }

            var (action, value) = editAction;
            if (string.IsNullOrWhiteSpace(value) && action != EditActionKind.Clear)
            {
                errors.AddInvalidProperty($"{propertyName}:{index}");
                active = false;
                continue;
            }

            switch (action)
            {
                case EditActionKind.Add:
                    Debug.Assert(value is not null); // action is not Clear, and we checked for it specifically.
                    if (active)
                        values.Add(value.ToLower());
                    break;

                case EditActionKind.Remove:
                    Debug.Assert(value is not null); // action is not Clear, and we checked for it specifically.
                    if (active)
                        values.Remove(value); // Since we're using a case insensitive comparer in the set, there's no need to call ToLower();
                    break;

                case EditActionKind.Clear:
                    if (active)
                        values.Clear();
                    break;
            }

            index++;
        }
    }

    public static void PerformActions<TValue>(
        this IEnumerable<EditAction<TValue>> actions,
        ICollection<TValue> values,
        ref ErrorList errors,
        IEqualityComparer<TValue>? comparer = null,
        EditActionCheck<TValue>? check = null,
        [CallerArgumentExpression(nameof(actions))] string propertyName = ""
    )
    {
        comparer ??= EqualityComparer<TValue>.Default;

        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(actions);
        bool active = true;

        int index = 0;
        foreach (var editAction in actions)
        {
            if (check?.Invoke(editAction, index) is ErrorMessage msg)
            {
                errors.AddError(msg);
                active = false;
                continue;
            }

            var (action, value) = editAction;

            switch (action)
            {
                case EditActionKind.Add:
                    if (comparer.Equals(value, default))
                    {
                        errors.AddInvalidProperty($"{propertyName}:{index}");
                        active = false;
                        continue;
                    }

                    Debug.Assert(value is not null);
                    if (active)
                        values.Add(value);
                    break;

                case EditActionKind.Remove:
                    if (comparer.Equals(value, default))
                    {
                        errors.AddInvalidProperty($"{propertyName}:{index}");
                        active = false;
                        continue;
                    }

                    Debug.Assert(value is not null); // action is not Clear, and we checked for it specifically.
                    if (active)
                    {
                        if (values.Remove(value) is false)
                        {
                            errors.AddEntityNotFound(typeof(TValue).Name, $"id:{value}");
                            active = false;
                            continue;
                        }
                    }
                    break;

                case EditActionKind.Clear:
                    if (comparer.Equals(value, default) is false)
                    {
                        errors.AddInvalidProperty($"{propertyName}:{index}");
                        active = false;
                        continue;
                    }

                    if (active)
                        values.Clear();
                    break;
            }

            index++;
        }
    }

    public static void PerformActions<TKey, TEntity>(this IEnumerable<EditAction<TKey>> actions, ICollection<TEntity> values, ref ErrorList errors, EditActionCheck<TKey>? check = null, [CallerArgumentExpression(nameof(actions))] string propertyName = "")
        where TKey : unmanaged, IEquatable<TKey>
        where TEntity : class, IKeyed<TEntity, TKey>
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(actions);
        bool active = true;

        Dictionary<TKey, TEntity> keyed = values.ToDictionary(k => k.Id, v => v);

        int index = 0;
        foreach (var editAction in actions)
        {
            if (check?.Invoke(editAction, index) is ErrorMessage msg)
            {
                errors.AddError(msg);
                active = false;
                continue;
            }

            var (action, value) = editAction;

            if (value.Equals(default) is not false && action != EditActionKind.Clear)
            {
                errors.AddInvalidProperty($"{propertyName}:{index}");
                active = false;
                continue;
            }

            if (keyed.TryGetValue(value, out var ent) is false)
            {
                errors.AddEntityNotFound(typeof(TEntity).Name, $"id:{value}");
                active = false;
                continue;
            }

            switch (action)
            {
                case EditActionKind.Add:
                    Debug.Assert(ent is not null); // action is not Clear, and we checked for it specifically.
                    if (active)
                        values.Add(ent);
                    break;

                case EditActionKind.Remove:
                    Debug.Assert(ent is not null); // action is not Clear, and we checked for it specifically.
                    if (active)
                        values.Remove(ent);
                    break;

                case EditActionKind.Clear:
                    if (active)
                        values.Clear();
                    break;
            }

            index++;
        }
    }
}
