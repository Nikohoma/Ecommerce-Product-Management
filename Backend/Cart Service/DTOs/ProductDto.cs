namespace CartService.DTOs
{
    public record ProductDto(int Id, string Name, string Description, decimal Price, List<ProductMediaDto>? Media);

    public record ProductMediaDto(string MediaUrl, string MediaType);
}
