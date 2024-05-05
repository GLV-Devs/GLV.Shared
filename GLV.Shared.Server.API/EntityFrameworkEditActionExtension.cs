using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GLV.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace GLV.Shared.Server.API;
public static class EntityFrameworkEditActionExtension
{
    public static async ValueTask<SuccessResult> PerformActions<TKey, TEntity, TUpdateModel>(this IEnumerable<EditAction<TUpdateModel>> actions, ICollection<TEntity> values, IQueryable<TEntity> entities, Func<TUpdateModel, TKey> keyGetter, Func<TUpdateModel, TEntity> entityTransform, EditActionCheck<TUpdateModel>? check = null, [CallerArgumentExpression(nameof(actions))] string propertyName = "")
        where TKey : unmanaged, IEquatable<TKey>
        where TEntity : class, IKeyed<TEntity, TKey>
        where TUpdateModel : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(keyGetter);
        ArgumentNullException.ThrowIfNull(entityTransform);

        bool active = true;
        ErrorList errors = new();
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

            if (value is null && action != EditActionKind.Clear)
            {
                errors.AddInvalidProperty($"{propertyName}:{index}");
                active = false;
                continue;
            }
            else if (action is EditActionKind.Clear)
            {
                if (value is not null)
                {
                    errors.AddInvalidProperty($"{propertyName}:{index}");
                    active = false;
                    continue;
                }

                values.Clear();
                continue;
            }

            Debug.Assert(value is not null);
            var key = keyGetter(value);
            if (keyed.TryGetValue(key, out var ent) is false && action is EditActionKind.Remove)
            {
                errors.AddInvalidProperty($"{propertyName}:{index}");
                active = false;
                continue;
            }
            else
            {
                ent = await entities.FirstOrDefaultAsync(x => x.Id.Equals(key)) ?? entityTransform.Invoke(value);
                if (ent is null && action == EditActionKind.Add)
                {
                    errors.AddInvalidProperty($"{propertyName}:{index}");
                    active = false;
                    continue;
                }
            }

            if (active)
            {
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
            }

            index++;
        }

        if (errors.Count > 0)
            return errors;
        return SuccessResult.Success;
    }
}
