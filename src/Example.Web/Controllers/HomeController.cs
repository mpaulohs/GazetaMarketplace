using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Example.Web.Models;

namespace Example.Web.Controllers
{
    /// <summary>
    /// Handles HTTP requests for the application's main user interface.
    /// </summary>
    /// <remarks>
    /// This controller serves as the entry point for the ASP.NET Core MVC application,
    /// providing actions for the home page, privacy policy, and error handling.
    /// It inherits from <see cref="Controller"/> to provide MVC functionality including
    /// view rendering, model binding, and HTTP response generation.
    /// </remarks>
#pragma warning disable CA1515 // Controllers must be public for MVC routing discovery
    public class HomeController : Controller
#pragma warning restore CA1515
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for this controller, injected by dependency injection.</param>
        /// <remarks>
        /// The logger is used throughout the controller lifetime to record diagnostic information,
        /// errors, and application events following the ASP.NET Core logging abstractions.
        /// </remarks>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Displays the application's home page.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> that renders the Index view to the client.
        /// </returns>
        /// <remarks>
        /// This action method is invoked when navigating to the root URL or /Home/Index.
        /// It returns a view result that renders Views/Home/Index.cshtml using the default layout.
        /// </remarks>
        /// <example>
        /// This action responds to the following HTTP requests:
        /// <code>
        /// GET /
        /// GET /Home
        /// GET /Home/Index
        /// </code>
        /// </example>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Displays the privacy policy page.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> that renders the Privacy view to the client.
        /// </returns>
        /// <remarks>
        /// This action method provides access to the application's privacy policy information.
        /// It returns a view result that renders Views/Home/Privacy.cshtml.
        /// </remarks>
        /// <example>
        /// This action responds to the following HTTP request:
        /// <code>
        /// GET /Home/Privacy
        /// </code>
        /// </example>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Displays a detailed error page with request tracking information.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> that renders the Error view with an <see cref="ErrorViewModel"/> containing the request ID.
        /// </returns>
        /// <remarks>
        /// This action is invoked when an unhandled exception occurs in the application.
        /// It provides diagnostic information including the request ID for troubleshooting.
        /// The ResponseCache attribute prevents caching of error pages to ensure users
        /// always see current error information.
        /// </remarks>
        /// <example>
        /// The error page includes the request ID which can be used to correlate with server logs:
        /// <code>
        /// Request ID: 0HMVFE0A2QJ4K:00000001
        /// </code>
        /// </example>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
