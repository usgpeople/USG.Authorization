using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace USG.Authorization
{
    public interface IWhitelist
    {
        bool Match(IPAddress ip);
    }

    public class Whitelist : IWhitelist
    {
        static Regex globToRegex(string glob)
        {
            return new Regex(
                "^" + Regex.Escape(glob).Replace("\\*", ".*?") + "$");
        }

        public static Whitelist Parse(string data)
        {
            var ips = new List<IPAddress>();
            var regexes = new List<Regex>();

            foreach (var line in data
                .Split('\n')
                .Select(s => Regex.Replace(s, "#.*", ""))
                .Select(s => s.Trim())
                .Where(s => s.Length != 0))
            {
                if (IPAddress.TryParse(line, out var ip))
                    ips.Add(ip);
                else
                    regexes.Add(globToRegex(line));
            }

            return new Whitelist(ips, regexes);
        }

        HashSet<IPAddress> _ips;
        Regex[] _regexes;

        public Whitelist(
            IEnumerable<IPAddress> ips,
            IEnumerable<Regex> regexes)
        {
            if (ips == null)
                throw new ArgumentNullException(nameof(ips));
            if (regexes == null)
                throw new ArgumentNullException(nameof(regexes));

            _ips = new HashSet<IPAddress>(ips);
            _regexes = regexes.ToArray(); 
        }

        public bool Match(IPAddress ip)
        {
            if (_ips.Contains(ip))
                return true;

            string ipString = ip.ToString();
            if (_regexes.Any(r => r.IsMatch(ipString)))
                return true;

            try
            {
                var host = Dns.GetHostEntry(ip);
                if (host.HostName != null &&
                        _regexes.Any(r => r.IsMatch(host.HostName)))
                    return true;
            }
            catch (SocketException)
            {
                // Lookup failed, ignore
            }

            return false;
        }
    }
}
