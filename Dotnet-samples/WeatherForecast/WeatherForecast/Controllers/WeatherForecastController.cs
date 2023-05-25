using Microsoft.AspNetCore.Mvc;

namespace WeatherForecast.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _serverId;
        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _serverId =_configuration.GetValue<string>("ServerId");
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        { 
            
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                ServerId = _serverId,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [Route("/gfn/app1")]
        public IActionResult GetApp1()
        {
            var appname = new
            {
                appID = "App1",
                ServerId = _serverId
            };
            return Ok(appname);
        }

        [Route("/gfn/app2")]
        public IActionResult GetApp2()
        {
            var appname = new
            {
                appID = "App2",
                ServerId = _serverId
            };
            return Ok(appname);
        }

        [Route("/gfn/clinicalLabelService")]
        public IActionResult GetOperator()
        {
            var appname = new
            {
                appID = "Hello!",
                ServerId = _serverId
            };
            return Ok(appname);
        }

        [Route("/")]
        public IActionResult Authorize()
        {
            _logger.Log(LogLevel.Information, "Authentication Call received");


            Request.Headers.TryGetValue("authorization", out var authorizationValue);
            if (!string.IsNullOrWhiteSpace(authorizationValue) && authorizationValue == "foo")
            {
                return Ok();
            }

            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }
}