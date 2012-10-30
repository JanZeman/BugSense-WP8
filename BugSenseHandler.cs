using BugSense.Coroutines;
using BugSense.Extensions;
using BugSense.Internal;
using BugSense.Notifications;
using BugSense.Tasks;
using BugSense_WP8;
using BugSense_WP8.Internal;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Net.Browser;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BugSense {
    public sealed class BugSenseHandler {

        #region [ Singleton ]

        BugSenseHandler()
        {

        }

        public static BugSenseHandler Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly BugSenseHandler instance = new BugSenseHandler();
        }

        #endregion

        #region [ Fields ]

        private NotificationOptions _options;
        private Application _application;
        private bool _initialized;
        private string _appVersion;
        private string _appName;
        public event EventHandler<BugSenseUnhandledExceptionEventArgs> UnhandledException;
        /// <summary>
        /// Occurs when the unhandled exception is sent to BugSense
        /// </summary>
        //public event EventHandler<BugSenseLogErrorCompletedEventArgs> UnhandledExceptionSent;

        #endregion

        #region [ Public Methods ]

///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use this method inside a catch block or when you want to send error details sto BugSense
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="tag"></param>
        /// <param name="options"></param>
        [Obsolete("Use BugSenseHandler.Instance.LogError")]
        public static void HandleError(Exception ex, string tag = null, NotificationOptions options = null)
        {
            Instance.LogError(ex, tag, options);
        }

        /// <summary>
        /// Use this method inside a catch block or when you want to send error details sto BugSense
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="tag"></param>
        /// <param name="options"></param>
        public void LogError(Exception ex, string tag = null, NotificationOptions options = null)
        {
            if (!_initialized)
                throw new InvalidOperationException("BugSense Handler is not initialized.");
            Handle(ex, 1, tag, options ?? Instance._options);
        }

        /// <summary>
        /// Initialized the BugSense handler. Must be called at App constructor.
        /// </summary>
        /// <param name="application">The Windows Phone application.</param>
        /// <param name="apiKey">The Api Key that can be retrieved at bugsense.com</param>
        /// <param name="options">Optional Options</param>
        public void Init(Application application, string apiKey, NotificationOptions options = null)
        {
            if (_initialized)
                return;

            //General Initializations
            G.timer.Restart();
            _options = options ?? GetDefaultOptions();
            _application = application;
            G.API_KEY = apiKey;

            SetExtraData();
            SetLogData();
            ResetBreadcrumbs();

            //Getting version and app details
            var appDetails = Helpers.GetVersion();
            _appName = appDetails[0];//nameHelper.Name;
            _appVersion = appDetails[1];//nameHelper.Version.ToString();
            //Proccess errors from previous crashes
            var tasks = new List<IResult> { new ProccessErrorsTask() };
            Coroutine.BeginExecute(tasks.GetEnumerator());

            //Attaching the handler
            if (_application != null)
            {
                _application.UnhandledException += OnUnhandledException;
            }

            SendEvent("_ping");

            //Just in case Init is called again
            _initialized = true;
        }

        /// <summary>
        /// If a Task was faulted, a handled exception is sent
        /// </summary>
        /// <param name="idstr">A string identifier for the Task.</param>
        /// <param name="t">The Task.</param>
        /// <param name="secs">Optional time in seconds to wait for the Task completion.</param>
        /// <returns></returns>
        public static void CheckTaskFault(string idstr, Task t, int secs = -1)
        {
            const int max = 4;
            int maxx = max;
            int snooze0 = 500;
            int i;

            if (secs > 0)
            {
                maxx = 1;
                snooze0 = secs * 1000;
            }

            Task tover = new Task(() =>
            {
                for (i = 0; i < maxx; i++)
                    if (t.IsCompleted)
                        break;
                    else
                    {
                        Thread.Sleep(snooze0);
                        snooze0 = 2 * snooze0;
                    }

                if (t.IsCompleted && t.IsFaulted)
                {
                    Instance.LogError(t.Exception.GetBaseException(), idstr, new NotificationOptions { Type = enNotificationType.None });
                }
            });
            tover.Start();
        }

        /// <summary>
        /// Gets the crash + handled exception extra data
        /// </summary>
        /// <returns>The extra data represented as a dictionary.</returns>
        public static Dictionary<string, string> GetAllExtraData(int handled)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(G.ExcExtraData.Get());

            if(handled>0)
                foreach (var pair in G.LogExtraData.Get())
                    res[pair.Key] = pair.Value;

            res["ms_from_start"] = G.timer.ElapsedMilliseconds.ToString();

            return res;
        }

        /// <summary>
        /// Resets the crash extra data
        /// </summary>
        /// <param name="dict">Optional dictionary to initialize the extra data with.</param>
        /// <returns></returns>
        public static void SetExtraData(Dictionary<string, string> dict = null)
        {
            lock (G.lockThis)
            {
                G.ExcExtraData.Set(dict);
            }
        }

        /// <summary>
        /// Adds a key-value pair to the crash extra data
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">Key's value.</param>
        /// <returns>true if the input pair was added, false otherwise.</returns>
        public static bool AddToExtraData(string key, string value)
        {
            lock (G.lockThis)
            {
                return G.ExcExtraData.AddTo(key, value);
            }
        }

        /// <summary>
        /// Removes a keyfrom the crash extra data
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>true if the input key was removed, false otherwise.</returns>
        public static bool RemoveFromExtraData(string key)
        {
            lock (G.lockThis)
            {
                return G.ExcExtraData.RemoveFrom(key);
            }
        }

        /// <summary>
        /// Resets the handled exception extra data
        /// </summary>
        /// <param name="dict">Optional dictionary to initialize the extra data with.</param>
        /// <returns></returns>
        public static void SetLogData(Dictionary<string, string> dict = null)
        {
            lock (G.lockThis)
            {
                G.ExcExtraData.Set(dict);
            }
        }

        /// <summary>
        /// Adds a key-value pair to the handled exception extra data
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">Key's value.</param>
        /// <returns>true if the input pair was added, false otherwise.</returns>
        public static bool AddToLogData(string key, string value)
        {
            lock (G.lockThis)
            {
                return G.ExcExtraData.AddTo(key, value);
            }
        }

        /// <summary>
        /// Resets the event chain (breadcrumbs)
        /// </summary>
        /// <returns></returns>
        public static void ResetBreadcrumbs()
        {
            lock (G.lockThis)
            {
                G.Breadcrumbs.Reset();
            }
        }

        /// <summary>
        /// Appends an event (breadcrumb) to the event chain
        /// </summary>
        /// <param name="evt">The event to append.</param>
        /// <returns>true if the bradcrumb was successfully left, false otherwise.</returns>
        public static bool LeaveBreadcrumb(string evt)
        {
            lock (G.lockThis)
            {
                return G.Breadcrumbs.AppendTo(evt);
            }
        }

        /// <summary>
        /// Returns the bradcrumbs left as a string
        /// </summary>
        /// <returns>String representation of the breadcrumbs left.</returns>
        public static string GetBreadcrumbs()
        {
            lock (G.lockThis)
            {
                return G.Breadcrumbs.Reduce();
            }
        }

///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets default options for error handling
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use BugSenseHandler.Instance.GetDefaultOptions")]
        public static NotificationOptions DefaultOptions()
        {
            return Instance.GetDefaultOptions();
        }

        public NotificationOptions GetDefaultOptions()
        {
            return new NotificationOptions {
                Title = Labels.DefaultNotificationTitle,
                Text = Labels.DefaultNotificationText_MessageBox,
                Type = enNotificationType.MessageBox
            };
        }

        #endregion

        #region [ Private Core Methods ]

        private void OnBugSenseUnhandledException(BugSenseUnhandledExceptionEventArgs e)
        {
            EventHandler<BugSenseUnhandledExceptionEventArgs> handler = UnhandledException;
            if (handler != null)
                handler(this, e);
        }


        private void OnUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is BugSenseUnhandledException)
                return;
            args.Handled = true;
            var e = new BugSenseUnhandledExceptionEventArgs(args.ExceptionObject, args.Handled);
            OnBugSenseUnhandledException(e);
            args.Handled = e.Handled;
            if (e.Cancel)
                return;
            if (Debugger.IsAttached && !_options.HandleWhileDebugging)
                return;
            Handle(args.ExceptionObject, 0, e.Comment, _options, !args.Handled);
            args.Handled = true;
        }

        private DateTime _lastMethodHandledCalledAt;
        private DateTime _lastFlatCalledAt;

        //TODO: Only live pings for now.
        private void SendEvent(string tag)
        {
            if (DateTime.Now.AddSeconds(-1) < _lastFlatCalledAt)
            {
                return;
            }
            _lastFlatCalledAt = DateTime.Now;

            AppEnvironment envie = GetEnvironment(true);
            var evtrequest = new BugSenseEvent(envie, tag);
            string contents = evtrequest.getFlatLine();

            Task sendIt = new Task(() => {
                try
                {
                    string uuid = ManageUUID();
                    string errorFlat = HttpUtility.UrlEncode(contents);
                    var request = WebRequestCreator.ClientHttp.Create(new Uri(G.EVT_URL_PRE + G.API_KEY + "/" + uuid));
                    // NOTE: for testing purposes
                    //var request = WebRequestCreator.ClientHttp.Create(new Uri(G.URL));

                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Headers[HttpRequestHeader.UserAgent] = "WP8";
                    request.Headers["X-BugSense-Api-Key"] = G.API_KEY;
                
                    request.BeginGetRequestStream(ar =>
                    {
                        try
                        {
                            var requestStream = request.EndGetRequestStream(ar);
                            using (var sw = new StreamWriter(requestStream))
                            {
                                sw.Write(ar.AsyncState);
                            }
                            request.BeginGetResponse(a =>
                            {
                            }, null);

                        }
                        catch
                        {
                        }
                    }, errorFlat);
                }
                catch
                { // Error is already saved so next time the app starts will try to send it again
                }
            });

            sendIt.Start();
        }

        private void Handle(Exception e, int handled, string tag, NotificationOptions options, bool throwExceptionAfterComplete = false)
        {
            if (DateTime.Now.AddSeconds(-1) < _lastMethodHandledCalledAt) {
                return;
            }
            _lastMethodHandledCalledAt = DateTime.Now;
            
            if (Debugger.IsAttached && !options.HandleWhileDebugging)//Dont send the error
                return;

            var request = new BugSenseRequest(e.ToBugSenseEx(handled, tag, GetBreadcrumbs()), GetEnvironment(), GetAllExtraData(handled));
            
            if (throwExceptionAfterComplete)
            {
                LogAndSend(request, true);
                return;
            }
            if (handled > 0)
            {
                LogAndSend(request);
                return;
            }

            try
            {
                switch (options.Type)
                {
                    case enNotificationType.MessageBox:
                        if (!NotificationBox.IsOpen())
                            NotificationBox.Show(options.Title, options.Text, new NotificationBoxCommand(Labels.OkMessage, () => { }));
                        LogAndSend(request);
                        break;
                    case enNotificationType.MessageBoxConfirm:
                        if (!NotificationBox.IsOpen())
                            Scheduler.Dispatcher.Schedule(
                                () =>
                                {
                                    try
                                    {
                                        if (!NotificationBox.IsOpen())
                                            NotificationBox.Show(options.Title, options.Text,
                                                                    new NotificationBoxCommand(Labels.OkMessage, () => LogAndSend(request)),
                                                                    new NotificationBoxCommand(Labels.CancelMessage, () => { }));
                                    }
                                    catch { }
                                });
                        break;
                    default:
                        LogAndSend(request);
                        break;
                }
            }
            catch (Exception)
            {
                if (options.Type != enNotificationType.MessageBoxConfirm)
                {
                    LogAndSend(request);
                }
            }
        }

        private void LogAndSend(BugSenseRequest request, bool throwAfterComplete = false)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                var eventArgs = new BugSenseLogErrorCompletedEventArgs(request, request.Exception != null ? request.Exception.OriginalException : null);
                //eventArgs.ExitApp = throwAfterComplete;
                var logTask = new LogErrorTask(request);
                var sendTask = new SendErrorTask();
                var tasks = new List<IResult> { logTask, sendTask };
                EventHandler<ResultCompletionEventArgs> die = (sender, args) => Scheduler.Dispatcher.Schedule(() =>
                {
                    throw new BugSenseUnhandledException();
                });
                EventHandler<ResultCompletionEventArgs> fix = (sender, args) => Scheduler.Dispatcher.Schedule(() =>
                {
                   ShowFixNotification();
     
                });
                Coroutine.BeginExecute(tasks.GetEnumerator(), callback: throwAfterComplete ? die : fix);
            });
        }

        #endregion

        #region [ Private Helper Methods ]

        private bool ShowFixNotification()
        {
            bool fix = false;
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (storage.DirectoryExists(G.FolderName))
                {
                    if (storage.FileExists(G.FIX_TMP_FILE))
                    {
                        using (var fileStream = storage.OpenFile(G.FIX_TMP_FILE, FileMode.Open))
                        {
                            try
                            {
                                DataContractJsonSerializer jsonDeserializer = new DataContractJsonSerializer(typeof(FixResponse));
                                FixResponse fixer = (FixResponse)jsonDeserializer.ReadObject(fileStream);
                               
                                if (!NotificationBox.IsOpen())
                                    NotificationBox.Show(fixer.Data.ContentTitle, fixer.Data.ContentText,
                                                            new NotificationBoxCommand(Labels.UpdateMessage, () =>  {
                                                                WebBrowserTask webBrowserTask = new WebBrowserTask();
                                                                webBrowserTask.Uri = new Uri(fixer.Data.Url);
                                                                webBrowserTask.Show();    
                                                            }), // go to upgrade
                                                            new NotificationBoxCommand(Labels.CancelMessage, () => { 
                                                            
                                                            })); // return to app
                                fix = true;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        storage.DeleteFile(G.FIX_TMP_FILE);
                    }
                }
            }

            return fix;
        }

        private AppEnvironment GetEnvironment(bool basic=false)
        {
            AppEnvironment environment = new AppEnvironment();
            environment.AppName = _appName;
            environment.AppVersion = _appVersion;
            environment.OsVersion = Environment.OSVersion.Version.ToString();

            string result = string.Empty;
            object manufacturer;
            if (DeviceExtendedProperties.TryGetValue("DeviceManufacturer", out manufacturer))
                result = manufacturer.ToString();
            environment.PhoneManufacturer = result;

            object theModel;
            if (DeviceExtendedProperties.TryGetValue("DeviceName", out theModel))
                result = result + theModel;
            //NOTE: result contains model + manufacturer. currently, for model we don't include the manufacturer.
            environment.PhoneModel = "" + theModel;

            environment.Locale = CultureInfo.CurrentCulture.EnglishName;

            environment.Uid = ManageUUID();

            if (basic)
                return environment;

            try {
                environment.ScreenHeight = _application.RootVisual.RenderSize.Height;
                environment.ScreenWidth = _application.RootVisual.RenderSize.Width;
            }
            catch { /* If the exception is not in the UIThread we don't have access to above */ }

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

        private static string ManageUUID()
        {
            string uuid = "";
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (storage.DirectoryExists(G.FolderName))
                {
                    if (storage.FileExists(G.UUID_FILE))
                    {
                        using (var fileStream = storage.OpenFile(G.UUID_FILE, FileMode.Open))
                        {
                            using (StreamReader sr = new StreamReader(fileStream))
                            {
                                uuid = sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            if (uuid.Length != G.UuidLen)
            {
                uuid = EntropyUUID.UUID.getNew();

                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (storage.DirectoryExists(G.FolderName))
                    {
                        using (var fileStream = storage.OpenFile(G.UUID_FILE, FileMode.OpenOrCreate))
                        {
                            using (StreamWriter sr = new StreamWriter(fileStream))
                            {
                                sr.Write(uuid);
                            }
                        }
                    }
                }
            }
            return uuid;
        }

        #endregion

    }
}
