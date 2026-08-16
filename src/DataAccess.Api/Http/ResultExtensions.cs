using DataAccess.Application.Common;

namespace DataAccess.Api.Http;

// Maps the application Result to an HTTP response: success carries the value/created resource,
// a failure is an expected business outcome and surfaces as 400 with its error message.
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
