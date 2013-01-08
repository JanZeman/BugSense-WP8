using System.Diagnostics;

namespace BugSense.Internal
{
    internal class G
    {
#if WINDOWS_PHONE
        internal const string BUGSENSE_NAME = "bugsense-wp8";
#elif NETFX_CORE
        internal const string BUGSENSE_NAME = "bugsense-w8";
#else
		internal const string BUGSENSE_NAME = "bugsense-dotnet";
#endif
        internal const string BUGSENSE_FLAVOR = "csharp";
        internal const string BUGSENSE_API_VER = "3.4";

        //NOTE: for testing purposes
#if DEBUG
        internal const string DEFAULT_URL = "http://requestb.in/1fvngws1";
        internal const string DEFAULT_PROXY_URL = DEFAULT_URL;
        internal const string DEFAULT_EVT_URL_PRE = DEFAULT_URL;
        internal const string DEFAULT_EVT_PROXY_URL_PRE = DEFAULT_URL;
        internal const bool DEFAULT_IS_EVT_URL_PRE = false;
#else
        internal const string DEFAULT_URL = "https://www.bugsense.com/api/errors";
        internal const string DEFAULT_PROXY_URL = "http://alt.bugsense.com/api/errors";
        internal const string DEFAULT_EVT_URL_PRE = "http://ticks2.bugsense.com/api/ticks/";
        internal const string DEFAULT_EVT_PROXY_URL_PRE = "http://alt.bugsense.com/api/ticks/";
        internal const bool DEFAULT_IS_EVT_URL_PRE = true;
#endif

        internal static string URL = DEFAULT_URL;
        internal static string PROXY_URL = DEFAULT_PROXY_URL;
        internal static string EVT_URL_PRE = DEFAULT_EVT_URL_PRE;
        internal static string EVT_PROXY_URL_PRE = DEFAULT_EVT_PROXY_URL_PRE;
        internal static bool IsEvtUrlPre = DEFAULT_IS_EVT_URL_PRE;
        internal static bool IsProxyActive = false;

        internal static string API_KEY;

#if (!WINDOWS_PHONE && !NETFX_CORE)
		internal static string API_BINARY_NAME;
#endif

        internal const string FolderName = "BugSense_Data";

        internal const int UuidLen = 40;
        internal static string UUID = "";
        internal const string UUIDFileName = "bugsense.udid";

        internal static System.Object LockThis = new System.Object();
        internal static ExtraData ExcExtraData = new ExtraData();
        internal static Breadcrumbs Breadcrumbs = new Breadcrumbs();

        internal static Stopwatch Timer = new Stopwatch();

        internal const string ExceptionFileNameSuffix = "_{0}_Ex_{1}.dat";
        internal const string EventFileNameSuffix = "_{0}_Ev_{1}.dat";

        internal const int MaxCrashes = 5;
        internal const string CrashFileNamePrefix = "CCC";
        internal const string CrashFileName = CrashFileNamePrefix + ExceptionFileNameSuffix;

        internal const int MaxLoggedExceptions = 8;
        internal const string LoggedExceptionFileNamePrefix = "LCC";
        internal const string LoggedExceptionFileName = LoggedExceptionFileNamePrefix + ExceptionFileNameSuffix;

        internal const int MaxPings = 2;
        internal const string PingFileNamePrefix = "PCC";
        internal const string PingFileName = PingFileNamePrefix + EventFileNameSuffix;

        internal const int MaxEvents = 12;
        internal const string EventFileNamePrefix = "ECC";
        internal const string EventFileName = EventFileNamePrefix + EventFileNameSuffix;

        internal const string PingEvent = "_ping";

#if (WINDOWS_PHONE || NETFX_CORE)
        internal static string LocalizedFixTitle = "";
        internal static string LocalizedFixText = "";
        internal static bool HasLocalizedFixes = false;
#endif
    }
}
