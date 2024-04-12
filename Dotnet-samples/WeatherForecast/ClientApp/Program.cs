// See https://aka.ms/new-console-template for more information

using System.Net;

var baseUrl = "http://localhost:5062/";
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri(baseUrl);


//var range = Enumerable.Range(0, 5);
//var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
//await Parallel.ForEachAsync(range, options, async (index, token) =>
//{
//    var result = await httpClient.GetAsync("/gfn/clinicalLabelService");
//    Console.WriteLine($"API invoked {index} : {result.StatusCode}");

//});

for (int iteration = 0; iteration < 5; iteration++)
{
    var result = await httpClient.GetAsync("/gfn/clinicalLabelService");
    Console.WriteLine($"API invoked {iteration} : {result.StatusCode}");
}


