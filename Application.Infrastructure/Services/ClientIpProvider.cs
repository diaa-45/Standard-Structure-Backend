using Application.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;

namespace Application.Infrastructure.Services
{
    public class ClientIpProvider : IClientIpProvider
    {
        private readonly IHttpContextAccessor _http;

        public ClientIpProvider(IHttpContextAccessor http)
        {
            _http = http;
        }

        public string GetClientIp()
        {
            try
            {
                var context = _http.HttpContext;
                if (context == null) return string.Empty;

                // Prefer X-Forwarded-For header
                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var values))
                {
                    var first = values.ToString().Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrWhiteSpace(first)) return NormalizeIp(first);
                }

                // Use connection remote ip
                var remote = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                return NormalizeIp(remote);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return string.Empty;

            if (IPAddress.TryParse(ip, out var addr))
            {
                // If IPv6 loopback or unspecified, attempt to get local network IP (non-loopback)
                if (IPAddress.IPv6Loopback.Equals(addr) || IPAddress.IsLoopback(addr))
                {
                    // try to find a non-loopback IPv4 address of the host
                    var hostIps = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    var candidate = hostIps.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (candidate != null) return candidate.ToString();
                    // fallback to IPv6 loopback mapped to ipv4 loopback
                    return IPAddress.Loopback.ToString();
                }

                // Map IPv6-mapped IPv4 to IPv4
                if (addr.IsIPv4MappedToIPv6 || (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && addr.ToString().StartsWith("::ffff:")))
                {
                    try
                    {
                        var v4 = addr.MapToIPv4();
                        return v4.ToString();
                    }
                    catch
                    {
                        return ip;
                    }
                }

                return addr.ToString();
            }

            return ip;
        }

    }
}