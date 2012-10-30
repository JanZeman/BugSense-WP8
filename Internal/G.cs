using System.Diagnostics;

namespace BugSense.Internal {
    internal class G {
        internal static string BUGSENSE_API_VER = "3.1";
        //internal static string URL = "http://requestb.in/tdb9uhtd";
        internal static string URL = "https://www.bugsense.com/api/errors";
        internal static string EVT_URL_PRE = "http://ticks2.bugsense.com/api/ticks/";
        internal static string API_KEY;

        internal static string FIX_TMP_FILE = "bugsense.response.cache";
        internal static string UUID_FILE = "bugsense.uuid";

        internal static int UuidLen = 40;

        internal static System.Object lockThis = new System.Object();
        internal static ExtraData ExcExtraData = new ExtraData();
        internal static ExtraData LogExtraData = new ExtraData();
        internal static Breadcrumbs Breadcrumbs = new Breadcrumbs();

        internal static Stopwatch timer = new Stopwatch();

        internal const int MaxExceptions = 5;
        internal const string FolderName = "BugSense_Exceptions";
        internal const string FileName = "CC_{0}_BugSense_Ex_{1}.dat";
    }
}
