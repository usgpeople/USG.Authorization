using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text.RegularExpressions;

namespace USG.Authorization.Tests
{
    [TestClass]
    public class WhitelistTest
    {
        [TestMethod]
        public void Match_Nothing()
        {
            var whitelist = new Whitelist(
                new IPAddress[] { },
                new Regex[] { });

            Assert.IsFalse(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Match_Ip()
        {
            var whitelist = new Whitelist(
                new IPAddress[] { IPAddress.Parse("127.0.0.1") },
                new Regex[] { });

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Match_Host()
        {
            var whitelist = new Whitelist(
                new IPAddress[] { },
                new Regex[] { new Regex("^google-public-dns-a\\.google\\.com$") });

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("8.8.8.8")));
        }

        [TestMethod]
        public void Match_Regex()
        {
            var whitelist = new Whitelist(
                new IPAddress[] { },
                new Regex[] { new Regex("^google-public-dns-[ab]\\.google\\.com$") });

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("8.8.8.8")));
        }

        [TestMethod]
        public void Match_LookupError()
        {
            var whitelist = new Whitelist(
                new IPAddress[] { },
                new Regex[] { });

            // 192.0.2.0/24 reserved for testing, should yield no host
            Assert.IsFalse(whitelist.Match(IPAddress.Parse("192.0.2.1")));
        }

        [TestMethod]
        public void Parse_Empty()
        {
            string input = "";

            var whitelist = Whitelist.Parse(input);

            Assert.IsFalse(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_Single()
        {
            string input = "127.0.0.1";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_SingleIPv6()
        {
            string input = "::1";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("::1")));
        }

        [TestMethod]
        public void Parse_Multiple()
        {
            string input = "127.0.0.1\n127.0.0.2";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.2")));
        }

        [TestMethod]
        public void Parse_WindowsNewlines()
        {
            string input = "127.0.0.1\r\n127.0.0.2";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.2")));
        }

        [TestMethod]
        public void Parse_Duplicate()
        {
            string input = "127.0.0.1\n127.0.0.1";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_Whitespace()
        {
            string input = "\n \t 127.0.0.1 \t \n";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_Comment()
        {
            string input = "# Allow localhost\n127.0.0.1";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_TrailingComment()
        {
            string input = "127.0.0.1 # Allow localhost";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_Hostname()
        {
            string input = "google-public-dns-a.google.com";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("8.8.8.8")));
        }

        [TestMethod]
        public void Parse_Glob()
        {
            string input = "*.google.com";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("8.8.8.8")));
        }

        [TestMethod]
        public void Parse_IpGlob()
        {
            string input = "127.0.0.*";

            var whitelist = Whitelist.Parse(input);

            Assert.IsTrue(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }

        [TestMethod]
        public void Parse_NoRegex()
        {
            string input = "[^\\.]+.google.com";

            var whitelist = Whitelist.Parse(input);

            Assert.IsFalse(whitelist.Match(IPAddress.Parse("8.8.8.8")));
        }

        [TestMethod]
        public void Parse_NoPartialMatch()
        {
            string input = "ocalhos";

            var whitelist = Whitelist.Parse(input);

            Assert.IsFalse(whitelist.Match(IPAddress.Parse("127.0.0.1")));
        }
    }
}
