using GLV.Shared.Server.Client.Models;

namespace GLV.Shared.Server;

public static class ResponseResultDataExtension
{
    public static async Task<ResponseResult> AssertSuccess(this Task<ResponseResult> result, string? errorMessage = null)
        => (await result).AssertSuccess(errorMessage);

    public static async Task<bool> GetIsSuccess(this Task<ResponseResult> result)
        => (await result).IsSuccess;

    public static async Task<ResponseResultData<T>> AssertSuccess<T>(this Task<ResponseResultData<T>> result, string? errorMessage = null)
        => (await result).AssertSuccess(errorMessage);

    public static async Task<T?> GetData<T>(this Task<ResponseResultData<T>> result, string? errorMessage = null)
        => (await result).GetData(errorMessage);
}
