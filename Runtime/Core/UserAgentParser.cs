using System;
using System.Text.RegularExpressions;

namespace TrackingLogSDK
{
    /// <summary>
    /// Parsed browser, host platform, and OS version extracted from a User-Agent string.
    /// </summary>
    public readonly struct ParsedUserAgent
    {
        public readonly string Browser;
        public readonly string Platform;
        public readonly string OsVersion;

        public ParsedUserAgent(string browser, string platform, string osVersion)
        {
            Browser = browser ?? string.Empty;
            Platform = platform ?? string.Empty;
            OsVersion = osVersion ?? string.Empty;
        }
    }

    /// <summary>
    /// Pure C# User-Agent parser for WebGL browser/platform metadata detection.
    /// </summary>
    public static class UserAgentParser
    {
        private static readonly Regex EdgeRegex = new Regex(@"Edg(?:A|iOS)?/(\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex OperaRegex = new Regex(@"(?:OPR|Opera)/(\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex FirefoxRegex = new Regex(@"(?:Firefox|FxiOS)/(\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex ChromeRegex = new Regex(@"(?:Chrome|CriOS)/(\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex SafariVersionRegex = new Regex(@"Version/(\d+(?:\.\d+)?)", RegexOptions.Compiled);

        private static readonly Regex WindowsNtRegex = new Regex(@"Windows NT (\d+\.\d+)", RegexOptions.Compiled);
        private static readonly Regex MacOsRegex = new Regex(@"Mac OS X (\d+[_\.]\d+(?:[_\.]\d+)?)", RegexOptions.Compiled);
        private static readonly Regex AndroidRegex = new Regex(@"Android (\d+(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex IosRegex = new Regex(@"(?:CPU (?:iPhone )?OS|iPad; CPU OS) (\d+[_\.]\d+(?:[_\.]\d+)?)", RegexOptions.Compiled);

        public static ParsedUserAgent Parse(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return new ParsedUserAgent("Unknown", "WebGL", string.Empty);
            }

            string browser = DetectBrowser(userAgent);
            string platform;
            string osVersion;
            DetectOs(userAgent, out platform, out osVersion);

            return new ParsedUserAgent(browser, platform, osVersion);
        }

        private static string DetectBrowser(string ua)
        {
            Match match;

            match = EdgeRegex.Match(ua);
            if (match.Success)
            {
                return "Edge " + match.Groups[1].Value;
            }

            match = OperaRegex.Match(ua);
            if (match.Success)
            {
                return "Opera " + match.Groups[1].Value;
            }

            match = FirefoxRegex.Match(ua);
            if (match.Success)
            {
                return "Firefox " + match.Groups[1].Value;
            }

            match = ChromeRegex.Match(ua);
            if (match.Success)
            {
                return "Chrome " + match.Groups[1].Value;
            }

            if (ua.IndexOf("Safari/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                match = SafariVersionRegex.Match(ua);
                return match.Success ? "Safari " + match.Groups[1].Value : "Safari";
            }

            return "Unknown";
        }

        private static void DetectOs(string ua, out string platform, out string osVersion)
        {
            Match match = WindowsNtRegex.Match(ua);
            if (match.Success)
            {
                platform = "Windows";
                string nt = match.Groups[1].Value;
                if (nt == "10.0")
                {
                    osVersion = "Windows 10/11";
                }
                else if (nt == "6.3")
                {
                    osVersion = "Windows 8.1";
                }
                else if (nt == "6.1")
                {
                    osVersion = "Windows 7";
                }
                else
                {
                    osVersion = "Windows NT " + nt;
                }
                return;
            }

            if (ua.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                platform = "Android";
                match = AndroidRegex.Match(ua);
                osVersion = match.Success ? "Android " + match.Groups[1].Value : "Android";
                return;
            }

            if (ua.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("iPod", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                platform = "iOS";
                match = IosRegex.Match(ua);
                osVersion = match.Success ? "iOS " + match.Groups[1].Value.Replace('_', '.') : "iOS";
                return;
            }

            match = MacOsRegex.Match(ua);
            if (match.Success || ua.IndexOf("Macintosh", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                platform = "MacOS";
                osVersion = match.Success ? "macOS " + match.Groups[1].Value.Replace('_', '.') : "macOS";
                return;
            }

            if (ua.IndexOf("Ubuntu", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                platform = "Ubuntu";
                osVersion = "Ubuntu";
                return;
            }

            if (ua.IndexOf("Linux", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                platform = "Linux";
                osVersion = "Linux";
                return;
            }

            platform = "WebGL";
            osVersion = string.Empty;
        }
    }
}
