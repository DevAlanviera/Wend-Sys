using System;
using System.Collections.Generic;

namespace Monobits.SharedKernel
{
    // This can be modified to BaseEntity<TId> to support multiple key types (e.g. Guid)
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<BaseDomainEvent> Events = new List<BaseDomainEvent>();
        public bool IsDeleted { get; private set; }
        public void Delete(bool isDeleted = true)
        {
            IsDeleted = isDeleted;
        }
    }
}