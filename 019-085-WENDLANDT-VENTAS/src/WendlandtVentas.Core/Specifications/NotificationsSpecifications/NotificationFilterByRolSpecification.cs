using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.MovementSpecifications
{
    public class NotificationFilterByRolSpecification : BaseSpecification<NotificationUser>
    {
        public NotificationFilterByRolSpecification(string usersId) : base(c => usersId.Equals(c.UserId))
        {
            AddInclude(c => c.Notification);
        }
    }
}
