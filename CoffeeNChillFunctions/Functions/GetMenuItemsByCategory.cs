using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using CoffeeNChillFunctions.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChillFunctions.Functions
{
    public class GetMenuItemsByCategory
    {
        private readonly ILogger<GetMenuItemsByCategory> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private const string TableName = "MenuItems";

        public GetMenuItemsByCategory(ILogger<GetMenuItemsByCategory> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _tableServiceClient.CreateTableIfNotExists(TableName);
        }


        // Code Attribution
        //
        //
        //
        //

        [Function("GetMenuItemsByCategory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequest req, string category)
        {
            _logger.LogInformation("Retrieving menu items for category: {Category}", category);

            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return new BadRequestObjectResult(new { message = "Category parameter is required." });
                }

                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var items = new List<MenuItem>();

                await foreach (var item in tableClient.QueryAsync<MenuItem>(x => x.PartitionKey == category))
                {
                    items.Add(item);
                }

                if (items.Count == 0)
                {
                    return new NotFoundObjectResult(new { message = $"No menu items found in category '{category}'." });
                }

                var dtos = items.Select(MenuItemDto.ToDto).Where(d => d != null).ToList();
                return new OkObjectResult(dtos);
            }
            catch (RequestFailedException ex) when (ex.Status == 404) // the 404 status code indicates that the table or category was not found
            {
                _logger.LogWarning(ex, "Table or category not found.");
                return new NotFoundObjectResult(new { message = $"Category '{category}' not found." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table Storage error encountered while retrieving category items.");
                return new ObjectResult(new { message = "Storage service error occurred while fetching items." })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu items by category.");
                return new ObjectResult(new { message = "An unexpected error occurred while retrieving menu items." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}