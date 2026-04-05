using Auth.Models;
using System;

    public enum ProductStatus
    {
        Draft,
        Submitted,
        Approved,
        Rejected,
        Active,
        Inactive
    }

    public class Product 
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        
        public int AvailableQuantity { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Draft;

    }
