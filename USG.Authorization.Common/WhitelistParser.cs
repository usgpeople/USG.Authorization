using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace USG.Authorization
{
    public static class WhitelistParser
    {
        public static ISet<IPAddress> Parse(string data)
        {
            return new HashSet<IPAddress>(data.Split('\n')
                .Select(s => Regex.Replace(s, "#.*", ""))
                .Select(s => s.Trim())
                .Where(s => s.Length != 0)
                .Select(IPAddress.Parse));
        }
    }
}
