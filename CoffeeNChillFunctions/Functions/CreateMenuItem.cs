using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class CreateMenuItem
    {
        private readonly ILogger _logger;
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string TableName = "MenuItems";

        public CreateMenuItem(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateMenuItem>();
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var item = JsonSerializer.Deserialize<MenuItem>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (item is null || string.IsNullOrWhiteSpace(item.PartitionKey) || string.IsNullOrWhiteSpace(item.RowKey))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("PartitionKey (Category) and RowKey (SKU) are required.");
                return bad;
            }

            var tableClient = new TableClient(ConnectionString, TableName);
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(item);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(item);
            return response;
        }
    }
}