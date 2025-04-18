﻿using MessagePack;
using System.Text.Json;

namespace GLV.Shared.Server.Client.Models;

/// <summary>
/// The JSON data transfer object for the bearer token response typically found in "/login" and "/refresh" responses.
/// </summary>
[MessagePackObject]
public sealed class GLVAccessTokenResponse
{
    /// <summary>
    /// The value is always "Bearer" which indicates this response provides a "Bearer" token
    /// in the form of an opaque <see cref="AccessToken"/>.
    /// </summary>
    /// <remarks>
    /// This is serialized as "tokenType": "Bearer" using <see cref="JsonSerializerDefaults.Web"/>.
    /// </remarks>
    [Key(0)]
    public string TokenType { get; } = "Bearer";

    /// <summary>
    /// The opaque bearer token to send as part of the Authorization request header.
    /// </summary>
    /// <remarks>
    /// This is serialized as "accessToken": "{AccessToken}" using <see cref="JsonSerializerDefaults.Web"/>.
    /// </remarks>
    [Key(1)]
    public required string AccessToken { get; init; }

    /// <summary>
    /// The number of seconds after the Unix Epoch at which the <see cref="AccessToken"/> expires.
    /// </summary>
    /// <remarks>
    /// This is serialized as "expiresIn": "{ExpiresInSeconds}" using <see cref="JsonSerialzierDefaults.Web"/>.
    /// </remarks>
    [Key(2)]
    public required long ExpiresIn { get; init; }

    /// <summary>
    /// If set, this provides the ability to get a new access_token after it expires using a refresh endpoint.
    /// </summary>
    /// <remarks>
    /// This is serialized as "refreshToken": "{RefreshToken}" using using <see cref="JsonSerializerDefaults.Web"/>.
    /// </remarks>
    [Key(3)]
    public required string RefreshToken { get; init; }
}
