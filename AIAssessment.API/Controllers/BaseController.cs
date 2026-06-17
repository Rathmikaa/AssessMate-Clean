using AIAssessment.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AIAssessment.API.Controllers
{
    /// <summary>
    /// All controllers inherit from this.
    /// Provides a single ToResponse() method that wraps every Result
    /// into the same consistent JSON shape.
    ///
    /// Every API response — success or error — looks like:
    /// {
    ///   "statusCode":    200,
    ///   "isSuccess":     true,
    ///   "messages":      ["Login successful."],
    ///   "errorMessages": [],
    ///   "body":          { ... your DTO ... }
    /// }
    /// </summary>
   


    [ApiController]
    public class BaseController : ControllerBase
    {
        protected IActionResult ToResponse(Result result)
        {
            return StatusCode(result.StatusCode, new
            {
                statusCode = result.StatusCode,
                isSuccess = result.IsSuccess,
                messages = result.Messages,
                errorMessages = result.ErrorMessages,
                body = result.Body
            });
        }
    }
}