using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Data;

public delegate ErrorMessage? EditActionCheck<T>(EditAction<T> editAction, int index);
public readonly record struct EditAction<T>(EditActionKind ActionKind, T? Value);
