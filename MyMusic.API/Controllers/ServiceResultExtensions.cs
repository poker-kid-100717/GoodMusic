using Microsoft.AspNetCore.Mvc;
using MyMusic.Core.Services;

namespace MyMusic.API.Controllers;

internal static class ServiceResultExtensions
{
    /// <summary>Maps a failed service result to the matching problem response.</summary>
    public static ActionResult ToProblem<T>(this ControllerBase controller, ServiceResult<T> result) => result.Status switch
    {
        ServiceStatus.NotFound => controller.NotFound(),
        ServiceStatus.Conflict => controller.Problem(result.Error, statusCode: StatusCodes.Status409Conflict),
        ServiceStatus.Invalid => controller.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest),
        _ => throw new InvalidOperationException($"Result {result.Status} is not a failure.")
    };
}
