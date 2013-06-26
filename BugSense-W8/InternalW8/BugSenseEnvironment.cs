using BugSense.Internal;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace BugSense.InternalW8
{
    internal class BugSenseEnvironment
    {
        public static AppEnvironment GetEnvironment(string appName, string appVersion, string uuid,
            bool basic = false)
        {
            AppEnvironment environment = new AppEnvironment();

            environment.AppName = appName;
            environment.AppVersion = appVersion;

            environment.OsVersion = "Windows NT 6.2";
            environment.PhoneModel = "unknown";
            environment.PhoneManufacturer = "unknown";

            Task.Run(async () => environment.CpuModel = await GetCpu()).Wait();
            environment.CpuBitness = sizeof(long) * 8;

            environment.IsTrial = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.IsTrial;

            environment.Uid = uuid;

            environment.GeoRegion = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;
            environment.Locale = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

            if (basic)
                return environment;

            environment.WifiOn = 2;
            environment.GpsOn = "unknown";
            environment.CellularData = "unknown";
            environment.Carrier = "unknown";

            environment.ScreenOrientation = Windows.Graphics.Display.DisplayProperties.CurrentOrientation.ToString();
            environment.ScreenDpi = Windows.Graphics.Display.DisplayProperties.LogicalDpi.ToString();
            
            var currentWindow = Windows.UI.Xaml.Window.Current;
            if (currentWindow != null)
            {
                environment.ScreenWidth = currentWindow.Bounds.Width;
                environment.ScreenHeight = currentWindow.Bounds.Height;
            }

            environment.Rooted = false;

            return environment;
        }

        private async static Task<String> GetCpu()
        {
            try
            {
                var selector = "System.Devices.InterfaceClassGuid:=\"{97FADB10-4E33-40AE-359C-8BEF029DBDD0}\"";
                Windows.Devices.Enumeration.DeviceInformationCollection ifaces =
                    await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(selector, null);
                if (ifaces != null)
                    return ifaces[0].Name;
            }
            catch (Exception)
            {
            }

            return "unknown";
        }

        // from: http://www.michielpost.nl/PostDetail_74.aspx
        // don't use it in GetEnvironment() method - it'll crash (cannot create a WebView yet)
        private static Task<string> GetUserAgent()
        {
            var tcs = new TaskCompletionSource<string>();
            WebView webView = new WebView();

            string htmlFragment = @"<html>

                    <head>
                        <script type='text/javascript'>
                            function GetUserAgent()
                            {
                                return navigator.userAgent;
                            }
                        </script>
                    </head>
                </html>";
            webView.LoadCompleted += (sender, e) =>
            {
                try
                {
                    // Invoke the javascript when the html load is complete
                    string result = webView.InvokeScript("GetUserAgent", null);

                    // Set the task result
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }

            };

            // Load Html
            webView.NavigateToString(htmlFragment);

            return tcs.Task;
        }

        private async static Task<string> GetOsVersionAsync()
        {

            string userAgent = await GetUserAgent();

            string result = string.Empty;

            // Parse user agent
            int startIndex = userAgent.ToLower().IndexOf("windows");
            if (startIndex > 0)
            {
                int endIndex = userAgent.IndexOf(";", startIndex);

                if (endIndex > startIndex)
                    result = userAgent.Substring(startIndex, endIndex - startIndex);
            }

            return result;
        }
    }
}
