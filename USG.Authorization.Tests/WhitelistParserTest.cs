using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;

namespace USG.Authorization.Tests
{
    [TestClass]
    public class WhitelistParserTest
    {
        [TestMethod]
        public void Parse_Empty()
        {
            string input = "";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_Single()
        {
            string input = "127.0.0.1";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { IPAddress.Parse("127.0.0.1") },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_SingleIPv6()
        {
            string input = "::1";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { IPAddress.Parse("::1") },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_Multiple()
        {
            string input = "127.0.0.1\n127.0.0.2";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[]
                {
                    IPAddress.Parse("127.0.0.1"),
                    IPAddress.Parse("127.0.0.2")
                },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_WindowsNewlines()
        {
            string input = "127.0.0.1\r\n127.0.0.2";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[]
                {
                    IPAddress.Parse("127.0.0.1"),
                    IPAddress.Parse("127.0.0.2")
                },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_Duplicate()
        {
            string input = "127.0.0.1\n127.0.0.1";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { IPAddress.Parse("127.0.0.1") },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_Whitespace()
        {
            string input = "\n \t 127.0.0.1 \t \n";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { IPAddress.Parse("127.0.0.1") },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_Comment()
        {
            string input = "# Allow localhost\n127.0.0.1";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { IPAddress.Parse("127.0.0.1") },
                whitelist.ToArray());
        }

        [TestMethod]
        public void Parse_TrailingComment()
        {
            string input = "127.0.0.1 # Allow localhost";

            var whitelist = WhitelistParser.Parse(input);

            CollectionAssert.AreEquivalent(
                new IPAddress[] { IPAddress.Parse("127.0.0.1") },
                whitelist.ToArray());
        }
    }
}
