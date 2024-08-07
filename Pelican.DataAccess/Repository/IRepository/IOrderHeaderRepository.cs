using Pelican.Models;

namespace Pelican.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader obj);
        void UpdateStatus(int id, string orderStatus, string? paymentStatus = null);
        void UpdateECPayPaymentId(int id, string sessionId, string paymentIntentId);
    }
}
