using Shared.Contracts;

namespace CatalogService.Services.Messaging
{
    public interface IPublisherForReport
    {
        Task SendProductForReporting(ProductStatusChangedEvent evt);
        Task SendProductActivityForReporting(ProductActivityEvent evt);
    }
}
