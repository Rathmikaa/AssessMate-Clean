using System.Collections.Generic;

namespace AIAssessment.Application.Common
{
    /// <summary>
    /// Unified response wrapper used by all Application services.
    ///
    /// Instead of throwing exceptions for expected failures or returning
    /// raw objects, every service method returns a Result.
    /// The controller reads StatusCode and decides the HTTP response.
    ///
    /// USAGE IN A SERVICE:
    ///   return new Result().GetResponse(dto, 200);
    ///   return new Result().GetErrorResponse(404, ["Assessment not found."]);
    ///
    /// USAGE IN A CONTROLLER:
    ///   var result = await _service.CreateAsync(dto);
    ///   return StatusCode(result.StatusCode, result.IsSuccess ? result.Body : result.ErrorMessages);
    /// </summary>
    public class Result
    {
        private static readonly List<string> _emptyErrors = [];

        /// <summary>The response payload — your DTO or object.</summary>
        public object? Body { get; set; }

        /// <summary>Business error messages shown to the client.</summary>
        public List<string> ErrorMessages { get; set; } = _emptyErrors;

        /// <summary>Success or informational messages.</summary>
        public List<string> Messages { get; set; } = [];

        /// <summary>HTTP status code — 200, 201, 400, 404, 409 etc.</summary>
        public int StatusCode { get; set; }

        /// <summary>True when StatusCode is in the 2xx range.</summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        /// <summary>Build a success response with a body.</summary>
        public Result GetResponse(object? body, int statusCode, List<string>? messages = null)
        {
            return new Result
            {
                Body = body,
                StatusCode = statusCode,
                Messages = messages ?? []
            };
        }

        /// <summary>Build an error response.</summary>
        public Result GetErrorResponse(int statusCode, List<string>? errorMessages = null)
        {
            return new Result
            {
                StatusCode = statusCode,
                ErrorMessages = errorMessages ?? []
            };
        }
    }
}