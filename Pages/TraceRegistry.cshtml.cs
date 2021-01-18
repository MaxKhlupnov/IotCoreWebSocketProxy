using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IotCoreWebSocketProxy.Pages
{
    public class TraceRegistryModel : PageModel
    {
        private readonly ILogger<TraceRegistryModel> _logger;

        public TraceRegistryModel(ILogger<TraceRegistryModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
