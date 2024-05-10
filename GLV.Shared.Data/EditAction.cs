using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Data;

public delegate ErrorMessage? EditActionCheck<T>(EditAction<T> editAction, int index);
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

    public static void PerformActions<T>(this IEnumerable<EditAction<T>> actions, ICollection<T> values, ref ErrorList errors, EditActionCheck<T>? check = null, [CallerArgumentExpression(nameof(actions))] string propertyName = "")
        where T : IEquatable<T>
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

            if (value?.Equals(default) is not false && action != EditActionKind.Clear)
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
                        values.Add(value);
                    break;

                case EditActionKind.Remove:
                    Debug.Assert(value is not null); // action is not Clear, and we checked for it specifically.
                    if (active)
                        values.Remove(value);
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

public readonly record struct EditAction<T>(EditActionKind ActionKind, T? Value);
