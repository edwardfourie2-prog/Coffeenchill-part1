using System.Net;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions.Functions
{
    public class DeleteMenuItem
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string TableName = "MenuItems";

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            var tableClient = new TableClient(ConnectionString, TableName);

            try
            {
                await tableClient.DeleteEntityAsync(category, id);
                var response = req.CreateResponse(HttpStatusCode.NoContent);
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