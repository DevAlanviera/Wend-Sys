using Xunit;
using WendlandtVentas.Web.Extensions;
using System;

namespace WendlandtVentas.Tests.Functionality
{
    public class FormatShould
    {
        [Fact]
        public void ValidateFormatCurrency()
        {
            decimal value = 100.0M;
            Assert.True(value.FormatCurrency().Equals("$100.00"));
        }

        [Fact]
        public void ValidateFormatCommasDecimals()
        {
            decimal value = 100000000M;      
            Assert.True(value.FormatCommasTwoDecimals().Equals("100,000,000.00")); 

            value = 100000000.11111111111111111111M;
            Assert.True(value.FormatCommasNullableTwoDecimals().Equals("100,000,000.11"));
        }

        [Fact]
        public void ValidateFormatCommasWhitoutDecimals()
        {
            int valueInt = 100000000;
            Assert.True(valueInt.FormatCommas().Equals("100,000,000"));

            decimal valueDouble = 100000000M;
            Assert.True(valueDouble.FormatCommasNullableTwoDecimals().Equals("100,000,000"));
        }

        [Fact]
        public void ValidateFormatDateLongMx()
        {
            var value = new DateTime(2020, 02, 28);
            var valueFormat = "viernes, febrero 28, 2020";

            Assert.True(value.FormatDateLongMx().Equals(valueFormat));
        }
    }
}