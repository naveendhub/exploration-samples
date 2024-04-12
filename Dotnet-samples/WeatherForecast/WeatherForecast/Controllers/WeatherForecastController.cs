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
        private static AsyncLocal<string> asyncLocalString = new AsyncLocal<string>();

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
        static async Task AsyncMethod(int count)
        {
            Helper.CustomLog($"AsyncMethod-Entering-{count}: {asyncLocalString.Value}");

            await Task.Delay(1).ConfigureAwait(false);

            Helper.CustomLog($"AsyncMethod-Exiting-{count}: {asyncLocalString.Value}");
        }

        [Route("/gfn/clinicalLabelService")]
        public async Task<IActionResult> GetDicomData()
        {
            asyncLocalString.Value = "Value1";
            
            Helper.CustomLog($"API-Start: {asyncLocalString.Value}");

            await AsyncMethod(1).ConfigureAwait(false);

            asyncLocalString.Value = "Value2";

            await AsyncMethod(2).ConfigureAwait(false);

            Helper.CustomLog($"API-End: {asyncLocalString.Value}");

            //var mainTask = Task.Factory.StartNew(() =>
            //{
            //    asyncLocalString.Value = "Value1";
            //    Helper.CustomLog($"MainTask: {asyncLocalString.Value}");

            //    var innerTask = Task.Factory.StartNew(() =>
            //    {
            //        Task.Delay(2000);
            //        Helper.CustomLog($"InnerTask1: {asyncLocalString.Value}");

            //    });

            //    asyncLocalString.Value = "Value2";

            //    var innerTask2 = Task.Factory.StartNew(() =>
            //    {
            //        Task.Delay(1000);
            //        Helper.CustomLog($"InnerTask2: {asyncLocalString.Value}");

            //    });

            //    asyncLocalString.Value = "Value3";

            //    var innerTask3 = Task.Factory.StartNew(() =>
            //    {
            //        Helper.CustomLog($"InnerTask3: {asyncLocalString.Value}");

            //    });

            //    Helper.CustomLog($"MainTask-End: {asyncLocalString.Value}");

            //});

            return Ok();
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