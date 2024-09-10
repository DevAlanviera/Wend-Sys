using System;
using System.Collections.Generic;
using System.Globalization;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Specifications.MovementSpecifications
{
    public class ListByFiltersMovementSpecification : BaseSpecification<Movement>
    {
        public ListByFiltersMovementSpecification(Dictionary<string, string> filter) : base(c => true)
        {

            if (!string.IsNullOrEmpty(filter["ProductPresentationId"]))
                AppendCriteria(c => c.ProductPresentationId == Convert.ToInt32(filter["ProductPresentationId"]), true);

            if (!string.IsNullOrEmpty(filter["DateStart"]))
            {
                var dateStart = DateTime.ParseExact(filter["DateStart"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
                AppendCriteria(c => c.CreatedAt.Date >= dateStart.ToUniversalTime().Date, true);
            }

            if (!string.IsNullOrEmpty(filter["DateEnd"]))
            {
                var dateEnd = DateTime.ParseExact(filter["DateEnd"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
                AppendCriteria(c => c.CreatedAt.Date <= dateEnd.ToUniversalTime().Date, true);
            }

            if (!string.IsNullOrEmpty(filter["UserId"]))
                AppendCriteria(c => c.UserId.Equals(filter["UserId"]), true);

            if (!string.IsNullOrEmpty(filter["Operation"]))
            {
                var operation = (Operation)Enum.Parse(typeof(Operation), filter["Operation"]);
                AppendCriteria(c => c.Operation == operation, true);
            }
        }
    }
}
