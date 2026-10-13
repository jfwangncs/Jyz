using System.ComponentModel;
using System.Globalization;
using Jyz.Constants;
using Jyz.Extensions;

namespace Jyz.UnitTests.Extensions
{
    public class EnumExtensionsTests
    {
        private enum Sample
        {
            [Description("first option")]
            First = 1,

            [Description("second option")]
            Second = 2,

            NoDescription = 3,
        }

        [Fact]
        public void GetDescription_ReturnsAttributeText_WhenPresent()
        {
            Sample.First.GetDescription().Should().Be("first option");
            Sample.Second.GetDescription().Should().Be("second option");
        }

        [Fact]
        public void GetDescription_FallsBackToEnumName_WhenAttributeMissing()
        {
            Sample.NoDescription.GetDescription().Should().Be(nameof(Sample.NoDescription));
        }

        [Theory]
        [InlineData("zh-CN", "成功")]
        [InlineData("en-US", "Success")]
        [InlineData("", "Success")]
        public void GetDescription_ReturnsLocalizedText_BasedOnCurrentUiCulture(string cultureName, string expected)
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
                ResponseCode.Success.GetDescription().Should().Be(expected);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}
