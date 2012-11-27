using System;
using System.Threading.Tasks;
using Windows.System;

namespace BugSense.Extensions
{
    internal class Browsers
    {
        public async static void Goto(string URI)
        {
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
        }
    }
}
