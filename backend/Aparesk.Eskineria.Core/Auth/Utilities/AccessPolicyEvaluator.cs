using System.Net;

namespace Aparesk.Eskineria.Core.Auth.Utilities;

public static class AccessPolicyEvaluator
{
    public static string? GetEmailDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return null;
        }

        var domain = email[(atIndex + 1)..].Trim();
        return string.IsNullOrWhiteSpace(domain)
            ? null
            : domain.ToLowerInvariant();
    }

    public static bool IsDomainAllowed(string? email, string? allowedDomains, string? blockedDomains)
    {
        var domain = GetEmailDomain(email);
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        var blockedSet = ParseList(blockedDomains).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (blockedSet.Contains(domain))
        {
            return false;
        }

        var allowedSet = ParseList(allowedDomains).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (allowedSet.Count == 0)
        {
            return true;
        }

        return allowedSet.Contains(domain);
    }

    public static bool IsIpAllowed(string? requestIp, string? whitelist)
    {
        if (string.IsNullOrWhiteSpace(requestIp) || string.IsNullOrWhiteSpace(whitelist))
        {
            return false;
        }

        if (!IPAddress.TryParse(requestIp, out var requestAddress))
        {
            return false;
        }

        foreach (var entry in ParseList(whitelist))
        {
            if (entry.Contains('/', StringComparison.Ordinal))
            {
                if (TryParseCidr(entry, out var networkAddress, out var prefixLength) &&
                    IsInCidr(requestAddress, networkAddress, prefixLength))
                {
                    return true;
                }

                continue;
            }

            if (IPAddress.TryParse(entry, out var allowedAddress) &&
                AddressesEqual(requestAddress, allowedAddress))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsRoleAllowed(IEnumerable<string> roles, string? roleWhitelist)
    {
        if (roles == null)
        {
            return false;
        }

        var whitelist = ParseList(roleWhitelist)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (whitelist.Count == 0)
        {
            return false;
        }

        return roles.Any(role => whitelist.Contains(role));
    }

    public static IReadOnlyList<string> ParseList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static bool TryParseCidr(string value, out IPAddress networkAddress, out int prefixLength)
    {
        networkAddress = IPAddress.None;
        prefixLength = 0;

        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var parsedNetworkAddress) ||
            parsedNetworkAddress == null ||
            !int.TryParse(parts[1], out prefixLength))
        {
            return false;
        }

        networkAddress = parsedNetworkAddress;
        var maxPrefix = networkAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefix)
        {
            return false;
        }

        return true;
    }

    private static bool IsInCidr(IPAddress address, IPAddress networkAddress, int prefixLength)
    {
        if (!TryGetComparableAddresses(address, networkAddress, out var normalizedAddress, out var normalizedNetwork))
        {
            return false;
        }

        var addressBytes = normalizedAddress.GetAddressBytes();
        var networkBytes = normalizedNetwork.GetAddressBytes();
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var index = 0; index < fullBytes; index++)
        {
            if (addressBytes[index] != networkBytes[index])
            {
                return false;
            }
        }

        if (remainingBits <= 0)
        {
            return true;
        }

        var mask = (byte)~(255 >> remainingBits);
        return (addressBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }

    private static bool AddressesEqual(IPAddress left, IPAddress right)
    {
        if (!TryGetComparableAddresses(left, right, out var normalizedLeft, out var normalizedRight))
        {
            return false;
        }

        return normalizedLeft.Equals(normalizedRight);
    }

    private static bool TryGetComparableAddresses(
        IPAddress first,
        IPAddress second,
        out IPAddress normalizedFirst,
        out IPAddress normalizedSecond)
    {
        normalizedFirst = first;
        normalizedSecond = second;

        if (first.AddressFamily == second.AddressFamily)
        {
            return true;
        }

        if (first.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
            first.IsIPv4MappedToIPv6)
        {
            normalizedFirst = first.MapToIPv4();
        }

        if (second.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
            second.IsIPv4MappedToIPv6)
        {
            normalizedSecond = second.MapToIPv4();
        }

        return normalizedFirst.AddressFamily == normalizedSecond.AddressFamily;
    }
}
