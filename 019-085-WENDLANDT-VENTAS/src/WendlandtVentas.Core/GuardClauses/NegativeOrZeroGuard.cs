using System;

namespace Ardalis.GuardClauses
{
    public static class NegativeOrZeroGuard
    {
        public static void NegativeOrZeroCustom(this IGuardClause guardClause, double input, string parameterName)
        {
            if (input <= 0)
                throw new ArgumentException("Cannot be negative or zero", parameterName);
        }

        public static void NegativeOrZeroCustom(this IGuardClause guardClause, int input, string parameterName)
        {
            if (input <= 0)
                throw new ArgumentException("Cannot be negative or zero", parameterName);
        }
    }
}