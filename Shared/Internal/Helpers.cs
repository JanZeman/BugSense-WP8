using BugSense.Internal;
using System;
#if WINDOWS_PHONE
using System.IO;
#endif
using System.Text;
#if WINDOWS_PHONE
using System.Windows;
#endif

namespace BugSense.Extensions
{
    internal static class Helpers
    {
        public static BugSenseEx ToBugSenseEx(this Exception ex, string excMsg, bool handled,
            string tag = null, string breadcrumbs = null)
        {
            BugSenseEx be = new BugSenseEx();
            be.Tag = tag;
            be.ClassName = ex.GetType().FullName;
            be.DateOccured = DateTime.Now;
            be.Name = ex.Message;
            be.StackTrace = GetStackTrace(ex, excMsg);
            be.Where = "NA";
            be.OriginalException = ex;
            be.Breadcrumbs = breadcrumbs;
            be.Handled = handled ? 1 : 0;
            return be;
        }

        private static string GetStackTrace(Exception ex, string excMsg)
        {
            bool found = false;

            StringBuilder sb = new StringBuilder("not available");
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb = new StringBuilder(ex.StackTrace);
                found = true;
            }

            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                if (string.IsNullOrEmpty(innerEx.StackTrace))
                    sb.AppendLine("not available");
                else
                {
                    sb.AppendLine(innerEx.StackTrace);
                    found = true;
                }
                innerEx = innerEx.InnerException;
            }

            //HACK: i was desperate!
            if (!found && !string.IsNullOrEmpty(excMsg))
                return excMsg;

            return sb.ToString().Trim();
        }

#if NETFX_CORE
        private static string versionString(Windows.ApplicationModel.PackageVersion version)
        {
            return String.Format("{0}.{1}.{2}.{3}",
                                 version.Major, version.Minor, version.Build, version.Revision);
        }
#endif

        public static string[] GetVersion()
        {
#if WINDOWS_PHONE
            // Based on: http://bjorn.kuiper.nu/2011/10/01/wp7-notify-user-of-new-application-version/
            Uri manifest = new Uri("WMAppManifest.xml", UriKind.Relative);
            string version = "0.0.0.0";
            string title = string.Empty;
            try
            {
                var si = Application.GetResourceStream(manifest);
                if (si != null)
                {
                    using (StreamReader sr = new StreamReader(si.Stream))
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (line != null)
                            {
                                int i = line.IndexOf("Title=\"", StringComparison.InvariantCulture);
                                if (i >= 0)
                                {
                                    line = line.Substring(i + 7);
                                    int z = line.IndexOf("\"", StringComparison.Ordinal);
                                    if (z >= 0)
                                    {
                                        title = line.Substring(0, z);
                                    }
                                }
                            }
                            if (line != null)
                            {
                                int y = line.IndexOf("Version=\"", StringComparison.InvariantCulture);
                                if (y >= 0)
                                {
                                    int z = line.IndexOf("\"", y + 9, StringComparison.InvariantCulture);
                                    if (z >= 0)
                                    {
                                        version = line.Substring(y + 9, z - y - 9);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return new[] { title, version };
#elif NETFX_CORE
            string version = "0.0.0.0";
            string title = string.Empty;

            try
            {
                title = Windows.ApplicationModel.Package.Current.Id.Name;
                version = versionString(Windows.ApplicationModel.Package.Current.Id.Version);
            }
            catch (Exception)
            {
            }

            return new[] { title, version };
#endif
        }

        public static void SleepFor(int ms)
        {
#if WINDOWS_PHONE
            System.Threading.Thread.Sleep(ms);
#elif NETFX_CORE
            new System.Threading.ManualResetEvent(false).WaitOne(ms);
#endif
        }

        public static void Log(string message)
        {
#if WINDOWS_PHONE
            System.Diagnostics.Debugger.Log(3, "[BugSense]", message + "\n");
#elif NETFX_CORE
            System.Diagnostics.Debug.WriteLine("[BugSense] " + message);
#endif
        }
    }
}
