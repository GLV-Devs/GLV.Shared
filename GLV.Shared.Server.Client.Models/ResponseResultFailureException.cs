﻿using GLV.Shared.Data;

namespace GLV.Shared.Server.Client.Models;

[Serializable]
public class ResponseResultFailureException : Exception
{
    public ErrorMessage[]? Errors { get; }

    public ResponseResultFailureException() { }
    public ResponseResultFailureException(IEnumerable<ErrorMessage> errors, Exception inner) : this(SerializeErrorMessages(errors), inner) 
    {
        Errors = errors.ToArray();
    }

    public ResponseResultFailureException(IEnumerable<ErrorMessage> errors) : this(SerializeErrorMessages(errors))
    {
        Errors = errors.ToArray();
    }

    public ResponseResultFailureException(string message, IEnumerable<ErrorMessage> errors) : this(message + "::" + SerializeErrorMessages(errors))
    {
        Errors = errors.ToArray();
    }

    public ResponseResultFailureException(string message) : base(message) { }
    public ResponseResultFailureException(string message, Exception inner) : base(message, inner) { }

    private static string SerializeErrorMessages(IEnumerable<ErrorMessage> errors)
        => "-> " + string.Join("\n-> ", errors.Select(x => $"{x.Key}:{x.DefaultMessageEN}: {(x.Properties is not null ? string.Join("|", x.Properties.Select(x => $"{x.Key}: {x.Value}")) : null)}"));
}
