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


    public class GetAllMenuItems
    {
        private readonly ILogger<GetAllMenuItems> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private const string TableName = "MenuItems";

        public GetAllMenuItems(ILogger<GetAllMenuItems> logger, TableServiceClient tableServiceClient)
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

        [Function("GetAllMenuItems")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequest req)
        {
            _logger.LogInformation("Retrieving all menu items.");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var items = new List<MenuItem>();

                await foreach (var item in tableClient.QueryAsync<MenuItem>())
                {
                    items.Add(item);
                }

                if (items.Count == 0)
                {
                    return new NotFoundObjectResult(new { message = "No menu items found in the database." });
                }

                var dtos = items.Select(MenuItemDto.ToDto).Where(d => d != null).ToList();
                return new OkObjectResult(dtos);

                // the catch blocks below handle specific exceptions that may occur during the retrieval process
                // RequestFailedException is thrown for Azure Table Storage related errors
                // RequestFailedException with status 404 indicates that the table does not exist
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning(ex, "MenuItems table does not exist.");
                return new NotFoundObjectResult(new { message = "Menu storage table was not found." });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table Storage error encountered while retrieving all items.");
                return new ObjectResult(new { message = "Storage service error occurred while fetching items." })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all menu items.");
                return new ObjectResult(new { message = "An unexpected error occurred while retrieving menu items." })
                {
                 // Set the status code to 500 Internal Server Error, to indicate that something went wrong on the server side
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}