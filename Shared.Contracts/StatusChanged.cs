using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts
{
    public class ProductStatusChangedEvent
    {
        public int ProductId { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }

        public decimal Price { get; set; } = 0;
    }
}
