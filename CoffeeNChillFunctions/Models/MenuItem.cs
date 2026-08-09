using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions.Models
{
    public class MenuItem : ITableEntity
    {
        // PartitionKey = Category (e.g. "Hot Drinks")
        public string PartitionKey { get; set; } = string.Empty;

        public string RowKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }

        // Required by ITableEntity 
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}