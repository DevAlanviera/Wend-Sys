using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;

namespace WendlandtVentas.Core.Entities
{
    public class NotificationUser : BaseEntity, IAggregateRoot
    {       
        public int NotificationId { get; private set; }
        public Notification Notification { get; private set; }
        public string UserId { get; private set; }
        public bool Active { get; private set; }
        public NotificationUser()
        {
        }
        public NotificationUser(string userId)
        {
            Guard.Against.NullOrEmpty(userId, nameof(userId));

            UserId = userId;
            Active = true;
        }
        public void ToggleStatus()
        {
            Active = !Active;
        }
    }
}
