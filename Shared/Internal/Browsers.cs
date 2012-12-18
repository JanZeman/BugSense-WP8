using System;
using System.Threading.Tasks;
#if (WINDOWS_PHONE || NETFX_CORE)
using Windows.System;
#else
using System.Diagnostics;
#endif

namespace BugSense.Extensions
{
    internal class Browsers
    {
        public async static void Goto(string URI)
        {
#if (WINDOWS_PHONE || NETFX_CORE)
            try
            {
                var success = await Launcher.LaunchUriAsync(new Uri(URI, UriKind.Absolute));
                if (success)
                {
                    Helpers.Log("Browser successfully launched");
                }
                else
                {
                    Helpers.Log("Browser launched failed");
                }
            }
            catch (Exception)
            {
                Helpers.Log("Browser launched failed");
            }
#else
			await Task.Run (() => {
				string sUrl = URI;
				try
				{
					Process.Start(sUrl);
				}
				catch(Exception exc1)
				{
					// System.ComponentModel.Win32Exception is a known exception that occurs
					// when Firefox is default browser.  
					// It actually opens the browser but STILL throws this exception so we can just ignore it.
					// If not this exception, then attempt to open the URL in IE instead.
					if (exc1.GetType().ToString() != "System.ComponentModel.Win32Exception") {
						// sometimes throws exception so we have to just ignore
						// this is a common .NET bug that no one online really has a great reason for
						// so now we just need to try to open the URL using IE if we can.
						try
						{
							ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("IExplore.exe", sUrl);
							Process.Start(startInfo);
							startInfo = null;
						}
						catch (Exception)
						{
							// still nothing we can do so just show the error to the user here.
						}
					}
				}
			});
#endif
        }
    }
}
