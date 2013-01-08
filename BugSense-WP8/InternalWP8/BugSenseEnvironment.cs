using BugSense.Internal;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Device.Location;
using System.Globalization;
using System.Windows;

namespace BugSense.InternalWP8
{
    internal class BugSenseEnvironment
    {
        public static AppEnvironment GetEnvironment(string appName, string appVersion, string uuid,
            bool basic = false)
        {
            AppEnvironment environment = new AppEnvironment();

            environment.AppName = appName;
            environment.AppVersion = appVersion;

            environment.OsVersion = Environment.OSVersion.Version.ToString();

            environment.CpuModel = "unknown";
            environment.CpuBitness = sizeof(long) * 8;

            environment.IsTrial = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.IsTrial;

            string result = string.Empty;
            object manufacturer;
            if (DeviceExtendedProperties.TryGetValue("DeviceManufacturer", out manufacturer))
                result = manufacturer.ToString();
            environment.PhoneManufacturer = result;

            object theModel;
            if (DeviceExtendedProperties.TryGetValue("DeviceName", out theModel))
            {
                //NOTE: result contains model + manufacturer
                result = result + theModel;
            }
            //NOTE: currently, for model we don't include the manufacturer
            environment.PhoneModel = "" + theModel;

            environment.Locale = CultureInfo.CurrentCulture.EnglishName;

            environment.Uid = uuid;

            if (basic)
                return environment;

            try
            {
                environment.ScreenHeight = Application.Current.RootVisual.RenderSize.Height;
                environment.ScreenWidth = Application.Current.RootVisual.RenderSize.Width;
            }
            catch (Exception)
            {
                // If the exception is not in the UIThread we don't have access to above
            }

            string gps_on = "Unknown";
            if (GeoPositionPermission.Denied.Equals(true))
                gps_on = "Denied";
            else if (GeoPositionPermission.Granted.Equals(true))
            {
                if (GeoPositionStatus.Disabled.Equals(true))
                    gps_on = "Disabled";
                else if (GeoPositionStatus.Initializing.Equals(true))
                    gps_on = "Initializing";
                else if (GeoPositionStatus.NoData.Equals(true))
                    gps_on = "NoData";
                else if (GeoPositionStatus.Ready.Equals(true))
                    gps_on = "Ready";
            }
            environment.GpsOn = gps_on;

            string screen_orientantion = "unknown";
            try
            {
                if ((((PhoneApplicationFrame)Application.Current.RootVisual).Orientation & PageOrientation.Landscape) == PageOrientation.Landscape)
                    screen_orientantion = "Landscape";
                if ((((PhoneApplicationFrame)Application.Current.RootVisual).Orientation & PageOrientation.LandscapeLeft) == PageOrientation.LandscapeLeft)
                    screen_orientantion = "LandscapeLeft";
                if ((((PhoneApplicationFrame)Application.Current.RootVisual).Orientation & PageOrientation.LandscapeRight) == PageOrientation.LandscapeRight)
                    screen_orientantion = "LandscapeRight";
                if ((((PhoneApplicationFrame)Application.Current.RootVisual).Orientation & PageOrientation.Portrait) == PageOrientation.Portrait)
                    screen_orientantion = "Portrait";
                if ((((PhoneApplicationFrame)Application.Current.RootVisual).Orientation & PageOrientation.PortraitUp) == PageOrientation.PortraitUp)
                    screen_orientantion = "PortraitUp";
                if ((((PhoneApplicationFrame)Application.Current.RootVisual).Orientation & PageOrientation.PortraitDown) == PageOrientation.PortraitDown)
                    screen_orientantion = "PortraitDown";
            }
            catch (Exception)
            {
            }
            environment.ScreenOrientation = screen_orientantion;
            environment.ScreenDpi = "unavailable";

            environment.WifiOn =
                DeviceNetworkInformation.IsWiFiEnabled.Equals(true) ? 1 : 0;
            environment.CellularData =
                DeviceNetworkInformation.IsCellularDataEnabled.Equals(true) ? "true" : "false";
            environment.Carrier =
                DeviceNetworkInformation.CellularMobileOperator;

            environment.Rooted = false;

            return environment;
        }
    }
}
