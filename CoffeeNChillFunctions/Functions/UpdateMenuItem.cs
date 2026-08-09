using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CoffeeNChillFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class UpdateMenuItem
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string TableName = "MenuItems";

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var updates = JsonSerializer.Deserialize<MenuItem>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var tableClient = new TableClient(ConnectionString, TableName);

            try
            {
                var existing = await tableClient.GetEntityAsync<MenuItem>(category, id);
                var entity = existing.Value;

                if (updates != null)
                {
                    entity.Price = updates.Price;
                    entity.IsAvailable = updates.IsAvailable;
                }

                await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Item {id} in category {category} not found.");
                return notFound;
            }
        }
    }
}