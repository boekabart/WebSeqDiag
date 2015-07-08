using System;
using Xunit;

namespace WebSeqDiag.UnitTest
{
    public class ApiHelpersTests
    {
        [Fact]
        public void ReadStyleFromWsdLinesWorks()
        {
            var actual = ApiHelpers.ReadStyleFromWsdLines(new[] {"#style=Omegapple", "title Expired Title"});
            Styles? expected = Styles.Omegapple;
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void ReadStyleFromWsdLinesUnknown()
        {
            var actual = ApiHelpers.ReadStyleFromWsdLines(new[] { "#style=BadStyle", "title Expired Title" });
            Styles? expected = null;
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void ReadStyleFromWsdLinesNull()
        {
            var actual = ApiHelpers.ReadStyleFromWsdLines(new[] { "title Expired Title" });
            Styles? expected = null;
            Assert.Equal(expected, actual);
        }
    }
}
