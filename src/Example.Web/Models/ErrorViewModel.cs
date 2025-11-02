using System;

namespace Example.Web.Models
{
#pragma warning disable CA1515 // Models must be public for MVC view binding
    public class ErrorViewModel
#pragma warning restore CA1515
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
