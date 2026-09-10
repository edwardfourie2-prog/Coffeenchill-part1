using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeNChillFunctions.Models.DTOs
{
    public class MenuItemDto
    {
        public string Category { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public double Price { get; set; }

        public bool IsAvailable { get; set; } = true;

        // method to convert MenuItem to MenuItemDto
        public static MenuItemDto? ToDto(MenuItem menuItem)
        {
            if (menuItem == null)
                return null;

            return new MenuItemDto
            {
                Category = menuItem.PartitionKey,
                Sku = menuItem.RowKey,
                Name = menuItem.Name,
                Description = menuItem.Description,
                Price = menuItem.Price,
                IsAvailable = menuItem.IsAvailable
            };
        }
    }
}
