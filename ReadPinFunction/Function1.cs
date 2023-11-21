using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using System.Diagnostics.Metrics;
using System.Text;

namespace ReadPinFunction
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "getpin")] HttpRequest req,
            ILogger log,ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            IConfigurationRoot configuration;
            if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                // Load configuration settings from local.settings.json for local development.
                configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
            else
            {
                // Load configuration settings from Azure Application Settings for Azure deployment.
                configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();
            }

            string sbnamepsace = configuration["ServiceBusNameSpace"];
            string topic = configuration["servicebusTopic"];
            string subscription = configuration["servicebusSubscription"];

            ServiceBusClient client = new ServiceBusClient(sbnamepsace);
            ServiceBusReceiver receiver = client.CreateReceiver(topic, subscription);

            try
            {
                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                if(message != null)
                {
                    log.LogInformation($"{message.Body}");
                    receiver.CompleteMessageAsync(message);
                    return new JsonResult(Encoding.UTF8.GetString(message.Body));
                }
                log.LogInformation("no message in the subscription");
            }
            catch(Exception ex)
            {
                log.LogError("Error in receiving the message"+ ex.Message);
            }
            //string name = req.Query["name"];

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkResult();
        }
    }
}
