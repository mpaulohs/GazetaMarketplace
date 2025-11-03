using System;

namespace Example.Web.Models
{
    /// <summary>
    /// Represents the data model for the error view, providing request tracking information.
    /// </summary>
    /// <remarks>
    /// This view model is used by the Error action in HomeController to display diagnostic
    /// information when an unhandled exception occurs. It contains the request ID which can
    /// be used to correlate error pages with server-side logs for troubleshooting.
    /// </remarks>
#pragma warning disable CA1515 // Models must be public for MVC view binding
    public class ErrorViewModel
#pragma warning restore CA1515
    {
        /// <summary>
        /// Gets or sets the unique identifier for the HTTP request that resulted in an error.
        /// </summary>
        /// <value>
        /// A string containing the Activity ID or HttpContext TraceIdentifier.
        /// </value>
        /// <remarks>
        /// This ID can be used to search application logs and correlate the error page display
        /// with the corresponding server-side exception and diagnostic information.
        /// </remarks>
        public string RequestId { get; set; }

        /// <summary>
        /// Gets a value indicating whether the request ID should be displayed to the user.
        /// </summary>
        /// <value>
        /// <c>true</c> if the RequestId is not null or empty; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This property is used in the Error view to conditionally display the request ID
        /// only when diagnostic information is available. This prevents showing empty or
        /// misleading error information to end users.
        /// </remarks>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
