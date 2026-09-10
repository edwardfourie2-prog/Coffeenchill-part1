using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CoffeeNChillFunctions.Functions
{
    public class DeleteMenuItem
    {
        private readonly ILogger<DeleteMenuItem> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private const string TableName = "MenuItems";

        public DeleteMenuItem(ILogger<DeleteMenuItem> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _tableServiceClient.CreateTableIfNotExists(TableName);
        }


        // Code Attribution


        [Function("DeleteMenuItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{sku}")] HttpRequest req, string category, string sku)
        {
            _logger.LogInformation("Deleting menu item SKU: {Sku} in Category: {Category}", sku, category);

            try
            {
                if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(sku))
                {
                    return new BadRequestObjectResult(new { 
                        
                        message = "Category and SKU path parameters are required." });
                }

                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var response = await tableClient.GetEntityIfExistsAsync<MenuItem>(category, sku);

                if (!response.HasValue)
                {
                    return new NotFoundObjectResult(new {
                        message = $"Menu item with Category '{category}' and SKU '{sku}' was not found." });
                }

                await tableClient.DeleteEntityAsync(category, sku);

                return new OkObjectResult(new { 
                    message = "Menu item deleted successfully." });
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning(ex, "Target entity for deletion was not found.");
                return new NotFoundObjectResult(new { message = "Menu item not found in Table Storage." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table Storage error during deletion.");
                return new ObjectResult(new { message = "Storage service error." })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu item.");
                return new ObjectResult(new { message = "An unexpected error occurred while deleting the menu item." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}