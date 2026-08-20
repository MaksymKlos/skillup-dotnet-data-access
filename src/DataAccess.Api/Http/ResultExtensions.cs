using DataAccess.Application.Common;

namespace DataAccess.Api.Http;

// A failed Result is an expected business outcome, so it maps to 400 rather than an exception.
public static class ResultExtensions
{
    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location)
        => result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : Results.BadRequest(new { error = result.Error });

    public static IResult ToOk<T>(this Result<T> result)
        => result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });

    public static IResult ToNoContent(this Result result)
        => result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error });
}
