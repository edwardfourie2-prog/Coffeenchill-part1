using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions.Functions
{
    public class CreateMenuItem
    {
        // The connection string for the Azurite table storage and docker 
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


            // Validation for missing required fields 
            
            if (item is null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid. No details provided.");
                return badResponse;
            }

                if (string.IsNullOrWhiteSpace(item.PartitionKey))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid Request, missing PartitionKey.");
                return badResponse;
            }
            if (string.IsNullOrWhiteSpace(item.RowKey))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid Request, missing RowKey.");
                return badResponse;
            }
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid Request, missing item Name.");
                return badResponse;
            }
            if (item.Price < 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid. Please enter a valid item price.");
                return badResponse;
                    }





            // a try catch to add exception handling when inserting the new entity,
            // for if the entity already exists other errors.

            try
            {


                var tableClient = new TableClient(ConnectionString, TableName);
                await tableClient.CreateIfNotExistsAsync();
                // triggers a 409 error if the entity already exists 
                await tableClient.AddEntityAsync(item);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(item);
                return response;

            }catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
            {
                _logger.LogWarning($"Item with PartitionKey '{item.PartitionKey}' and RowKey '{item.RowKey}' already exists in the table");
                var InsertResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await InsertResponse.WriteStringAsync($"Menu item with SKU '{item.RowKey}' in category '{item.PartitionKey}' already exists in the table");
                return InsertResponse;

}
                catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding menu item to Azure Table Storage");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An unexpected error occurred while processing your request. Please try again");
                return errorResponse;
            }
        }
    }
}