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
using System.Text.Json;

namespace CoffeeNChillFunctions.Functions
{
    public class UpdateMenuItem
    {
        private readonly ILogger<UpdateMenuItem> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private const string TableName = "MenuItems";

        public UpdateMenuItem(ILogger<UpdateMenuItem> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _tableServiceClient.CreateTableIfNotExists(TableName);
        }


        // Code Attribution




        [Function("UpdateMenuItem")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{sku}")] HttpRequest req, string category, string sku)
        {
            _logger.LogInformation("Updating menu item SKU: {Sku} in Category: {Category}", sku, category);

            try
            {
                if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(sku))
                {
                    return new BadRequestObjectResult(new { message = "Category and SKU path parameters are required." });
                }

                var dto = await req.ReadFromJsonAsync<MenuItemDto>();
                if (dto == null)
                {
                    return new BadRequestObjectResult(new { message = "Invalid update payload format or empty body." });
                }

                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var response = await tableClient.GetEntityIfExistsAsync<MenuItem>(category, sku);

                if (!response.HasValue)
                {
                    return new NotFoundObjectResult(new { message = $"Menu item with Category '{category}' and SKU '{sku}' was not found." });
                }

                var entity = response.Value;
                entity.Name = string.IsNullOrWhiteSpace(dto.Name) ? entity.Name : dto.Name;
                entity.Description = dto.Description;
                entity.Price = dto.Price > 0 ? dto.Price : entity.Price;
                entity.IsAvailable = dto.IsAvailable;

                await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);

                return new OkObjectResult(MenuItemDto.ToDto(entity));
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning(ex, "Target menu item not found during update.");
                return new NotFoundObjectResult(new { message = "Menu item not found in Table Storage." });
            }
            catch (RequestFailedException ex) when (ex.Status == 412) // for missmatch
            {
                _logger.LogWarning(ex, "conflict during update for SKU: {Sku}", sku);
                return new ConflictObjectResult(new { message = "Item was updated by another process. Please re-fetch and try again." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table Storage communication error during update.");
                return new ObjectResult(new { message = "Storage service communication error." })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating menu item.");
                return new ObjectResult(new { message = "An unexpected error occurred while updating the menu item." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}