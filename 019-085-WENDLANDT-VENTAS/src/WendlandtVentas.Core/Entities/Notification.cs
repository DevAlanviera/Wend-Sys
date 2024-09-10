using Ardalis.GuardClauses;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace WendlandtVentas.Core.Entities
{
    public class Notification : BaseEntity, IAggregateRoot
    {
        public string Title { get; private set; }
        public string Message { get; private set; }
        public ICollection<NotificationUser> NotificationUsers { get; private set; }
        public Notification() { }
        public Notification(string title, string message,  IEnumerable<string> usersId)
        {
            Guard.Against.NullOrEmpty(title, nameof(title));
            Guard.Against.NullOrEmpty(message, nameof(message));
            Guard.Against.NullOrEmpty(usersId, nameof(usersId));

            Title = title;
            Message = message;
            NotificationUsers = usersId.Select(c => new NotificationUser(c)).ToList();
        }
    }
}
