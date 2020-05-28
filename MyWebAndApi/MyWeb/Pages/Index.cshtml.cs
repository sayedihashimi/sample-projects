using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MyWeb.Pages {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger) {
            _logger = logger;
        }

        public async Task OnGetAsync() {
            string baseurl = "https://localhost:5001";
            var httpclient = new HttpClient();
            swaggerClient client = new swaggerClient(baseurl, httpclient);
            var weather = await client.WeatherForecastAsync();
            Console.WriteLine(weather.ElementAt(0).Summary);
        }
    }
}
