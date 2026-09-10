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


    // Code Attribution 
    //
    //
    //
    //

    public class CreateMenuItem
    {

    //Required fields to use the Azurite emulator for local development
    private readonly TableServiceClient _tableServiceClient;

        private readonly ILogger _logger;
        private const string TableName = "MenuItems";

        public CreateMenuItem(ILogger<CreateMenuItem> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _tableServiceClient.CreateTableIfNotExists(TableName);
        }

    [Function("CreateMenuItem")]
    public async Task<IActionResult> Run(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequest req)
    {
        _logger.LogInformation("Creating a new menu item.");


        // Validate the incoming request payload
        // for example, check if the required fields are present and valid
        // If not, return a BadRequestObjectResult with an appropriate message
        try
        {
            var dto = await req.ReadFromJsonAsync<MenuItemDto>();

            if (dto == null)
            {
                return new BadRequestObjectResult(new { 
                    message = "Empty or invalid payload." 
                
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Name))
            {
                return new BadRequestObjectResult(new { message = "Category, Sku, and Name are required." });
            }

            if (dto.Price <= 0)
            {
                return new BadRequestObjectResult(new { message = "Price must be greater than zero." });
            }

            var entity = new MenuItem
            {
                PartitionKey = dto.Category,
                RowKey = dto.Sku,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                IsAvailable = dto.IsAvailable
            };

            // using the AddEntityAsync method to insert the entity into the table
            var tableClient = _tableServiceClient.GetTableClient(TableName);
            await tableClient.AddEntityAsync(entity);

            var resultDto = MenuItemDto.ToDto(entity);
            return new CreatedResult($"/api/menu/{entity.PartitionKey}/{entity.RowKey}", resultDto);

            // the Catch block is used to handle exceptions that may occur during the execution of the code.
            // In this case, this is catching a RequestFailedException with a status code of 409 (Conflict).
            // and returning a ConflictObjectResult with a message indicating that a menu item with the same Category and SKU already exists
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Duplicate menu item detected.");
            return new ConflictObjectResult(new { message = "A menu item with this Category and SKU already exists." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu item.");
            return new ObjectResult(new { message = "An error occurred while creating the menu item." })
            {
                // Set the status code to 500 Internal Server Error, to indicate that something went wrong on the server side
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
}