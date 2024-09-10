using System;

namespace Ardalis.GuardClauses
{
    public static class NegativeGuard
    {
        public static void NegativeCustom(this IGuardClause guardClause, double input, string parameterName)
        {
            if (input < 0)
                throw new ArgumentException("Cannot be negative", parameterName);
        }

        public static void NegativeCustom(this IGuardClause guardClause, int input, string parameterName)
        {
            if (input < 0)
                throw new ArgumentException("Cannot be negative", parameterName);
        }
    }
}