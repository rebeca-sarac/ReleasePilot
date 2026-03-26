using ErrorOr;

namespace ReleasePilot.Api.Errors;

internal static class ErrorOrExtensions
{
    internal static IResult ToProblemResult<T>(this ErrorOr<T> errorOr)
    {
        var error = errorOr.FirstError;

        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new { error.Code, error.Description }),
            ErrorType.Conflict => Results.Conflict(new { error.Code, error.Description }),
            ErrorType.Validation => Results.UnprocessableEntity(new { error.Code, error.Description }),
            ErrorType.Unauthorized => Results.Problem(detail: error.Description,
                                                      statusCode: StatusCodes.Status401Unauthorized),
            
            _ => Results.Problem(detail: error.Description,
                                 statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
