//NOTE: for testing purposes
#if DEBUG
#define FREE_BIRD
#endif

using BugSense.Extensions;
using BugSense.Internal;
#if WINDOWS_PHONE
using BugSense.InternalWP8;
#elif NETFX_CORE
using BugSense.InternalW8;
#else
using BugSense.InternalDotNet;
#endif
using BugSense.Tasks;
#if WINDOWS_PHONE
using BugSense_WP8;
#elif NETFX_CORE
using BugSense_W8;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
#if WINDOWS_PHONE
using System.Windows;
#elif NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Core;
#endif

namespace BugSense
{

    public sealed class BugSenseHandler
    {

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

        class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly BugSenseHandler instance = new BugSenseHandler();
        }

        #endregion

        #region [ Attributes ]

#if DEBUG
		public bool DebugFixResponse { get; set; }
#endif
        private Worker _worker;
        private NotificationOptions _options;
#if (WINDOWS_PHONE || NETFX_CORE)
        private Application _application;
#else
		private AppDomain _application;
#endif
		private bool _initialized;
        private string _appVersion;
        private string _appName;
        private static Action _actOnExit = null;
        public event EventHandler<BugSenseUnhandledExceptionEventArgs> UnhandledException;
#if NETFX_CORE
        private CoreDispatcher _dispatcher;
#endif

        #endregion

        #region [ Public Methods ]

        ///////////////////////////////////////////////////////////////////////////////////////////

#if (WINDOWS_PHONE || NETFX_CORE)
		/// <summary>
		/// Initialized the BugSense handler. Must be called at App constructor.
		/// </summary>
		/// <param name="application">The Windows application.</param>
		/// <param name="apiKey">The Api Key that can be retrieved from bugsense.com</param>
		/// <param name="options">Optional Options</param>
		public void Init(Application application, string apiKey, NotificationOptions options = null)
#else
		/// <summary>
		/// Initialized the BugSense handler. Must be called at App constructor.
		/// </summary>
		/// <param name="apiKey">The Api Key that can be retrieved from bugsense.com</param>
		/// <param name="options">Optional Options</param>
		public void Init(string apiKey, NotificationOptions options = null)
#endif
		{
#if DEBUG
			DebugFixResponse = false;
#endif

            if (_initialized)
                return;

            // General Initializations
            G.Timer.Restart();
            _options = options ?? GetDefaultOptions();
#if (WINDOWS_PHONE || NETFX_CORE)
			_application = application;
#else
			_application = AppDomain.CurrentDomain;
			G.API_BINARY_NAME = _application.FriendlyName;
#endif
			G.API_KEY = apiKey;

            SetExtraData();
            ResetBreadcrumbs();

            // Getting version and app details
            var appDetails = Helpers.GetVersion();
            _appName = appDetails[0];
            _appVersion = appDetails[1];

            // Proccess errors from previous crashes
            _worker = new Worker(true);
            _worker.FixNotification += new FixNotificationEventHandler(FixNotifier);

            // Attaching the handler
            if (_application != null)
            {
#if (WINDOWS_PHONE || NETFX_CORE)
                _application.UnhandledException += OnUnhandledException;
#else
				_application.UnhandledException +=
					new UnhandledExceptionEventHandler(OnUnhandledException);
#endif
#if NETFX_CORE
                TryToSetDispatcher();
#endif
            }

            _actOnExit = null;

            HandleEvent(G.PingEvent, false, true);

            // Just in case Init() is called again
            _initialized = true;
        }

        /// <summary>
        /// Use this method inside a catch block or when you want to send error details to BugSense
        /// </summary>
        /// <param name="ex">The logged exception object.</param>
        /// <param name="tag">Optional tag that accompanies the logged exception object.</param>
        public void LogException(Exception ex, string tag = null)
        {
            if (!_initialized)
                throw new InvalidOperationException("BugSense Handler is not initialized.");
            Handle(ex, "", false, tag, null, false);
        }

        /// <summary>
        /// Use this method inside a catch block or when you want to send error details to BugSense
        /// </summary>
        /// <param name="ex">The logged exception object.</param>
        /// <param name="key">Optional extra data key (for this exception only).</param>
        /// <param name="value">Optional extra data key's value.</param>
        /// <param name="tag">Optional tag that accompanies the logged exception object.</param>
        public void LogException(Exception ex, string key, string value, string tag = null)
        {
            if (!_initialized)
                throw new InvalidOperationException("BugSense Handler is not initialized.");
            Handle(ex, "", false, tag, null, false, new Dictionary<string, string>() { { key, value } });
        }

        /// <summary>
        /// Use this method inside a catch block or when you want to send error details to BugSense
        /// </summary>
        /// <param name="ex">The logged exception object.</param>
        /// <param name="logExtra">Optional extra data (for this exception only).</param>
        /// <param name="tag">Optional tag that accompanies the logged exception object.</param>
        public void LogException(Exception ex, Dictionary<string, string> logExtra, string tag = null)
        {
            if (!_initialized)
                throw new InvalidOperationException("BugSense Handler is not initialized.");
            Handle(ex, "", false, tag, null, false, logExtra);
        }

        /// <summary>
        /// If a Task was faulted, a handled exception is sent
        /// </summary>
        /// <param name="idstr">A string identifier for the Task.</param>
        /// <param name="t">The Task.</param>
        /// <param name="logExtra">Optional extra data (for this exception only).</param>
        /// <param name="secs">Optional time in seconds to wait for the Task completion.</param>
        /// <returns></returns>
        public static void CheckTaskFault(string idstr, Task t,
            Dictionary<string, string> logExtra = null, int secs = -1)
        {
            const int max = 5;
            int maxx = max;
            int snooze0 = 500;
            int i;

            if (!Instance._initialized)
                throw new InvalidOperationException("BugSense Handler is not initialized.");

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
                        Helpers.SleepFor(snooze0);
                        snooze0 = 2 * snooze0;
                    }

                if (t.IsCompleted && t.IsFaulted)
                {
                    Instance.Handle(t.Exception.GetBaseException(), "", true, idstr, null, false, logExtra);
                }
            });
            tover.Start();
        }

        /// <summary>
        /// Use this method to send an event to BugSense
        /// </summary>
        /// <param name="tag">The event.</param>
        public void SendEvent(string tag)
        {
            HandleEvent(tag, true, false);
        }

        /// <summary>
        /// Gets the crash extra data
        /// </summary>
        /// <returns>The extra data represented as a dictionary.</returns>
        public static Dictionary<string, string> GetExtraData()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            lock (G.LockThis)
            {
                res = new Dictionary<string, string>(G.ExcExtraData.Dict);
            }

            return res;
        }

        /// <summary>
        /// Resets the crash extra data
        /// </summary>
        /// <param name="dict">Optional dictionary to initialize the extra data with.</param>
        /// <returns></returns>
        public static void SetExtraData(Dictionary<string, string> dict = null)
        {
            lock (G.LockThis)
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
            bool res = false;

            lock (G.LockThis)
            {
                res = G.ExcExtraData.AddTo(key, value);
            }

            return res;
        }

        /// <summary>
        /// Removes a key from the crash extra data
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>true if the input key was removed, false otherwise.</returns>
        public static bool RemoveFromExtraData(string key)
        {
            bool res = false;

            lock (G.LockThis)
            {
                res = G.ExcExtraData.RemoveFrom(key);
            }

            return res;
        }

        /// <summary>
        /// Resets the event chain (breadcrumbs)
        /// </summary>
        /// <returns></returns>
        public static void ResetBreadcrumbs()
        {
            lock (G.LockThis)
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
            bool res = false;

            lock (G.LockThis)
            {
                res = G.Breadcrumbs.AppendTo(evt);
            }

            return res;
        }

        /// <summary>
        /// Returns the bradcrumbs left as a string
        /// </summary>
        /// <returns>String representation of the breadcrumbs left.</returns>
        public static string GetBreadcrumbs()
        {
            string res = "";

            lock (G.LockThis)
            {
                res = G.Breadcrumbs.ToString();
            }

            return res;
        }

        /// <summary>
        /// Gets default options for error handling
        /// </summary>
        /// <returns>The notification options.</returns>
        [Obsolete("Use BugSenseHandler.Instance.GetDefaultOptions")]
        public static NotificationOptions DefaultOptions()
        {
            return Instance.GetDefaultOptions();
        }

        /// <summary>
        /// Gets default options for error handling
        /// </summary>
        /// <returns>The notification options.</returns>
        public NotificationOptions GetDefaultOptions()
        {
            return new NotificationOptions
            {
                Type = enNotificationType.None
            };
        }

        /// <summary>
        /// Sets an action to perform when the application crashes
        /// (just before exiting to the OS).
        /// Caution! You shouldn't perform any async or UI routine calls in this action;
        /// the application will immediately exit after this call returns.
        /// </summary>
        /// <param name="act">The action to perform.</param>
        /// <returns></returns>
        public static void SetLastBreath(Action act)
        {
            _actOnExit = act;
        }

        /// <summary>
        /// Sends subsequent requests through the BugSense proxy
        /// </summary>
        /// <returns></returns>
        public static void ActivateProxy()
        {
            G.IsProxyActive = true;
        }

        /// <summary>
        /// Cancels sending subsequent requests through the BugSense proxy
        /// </summary>
        /// <returns></returns>
        public static void DeactivateProxy()
        {
            G.IsProxyActive = false;
        }

#if (WINDOWS_PHONE || NETFX_CORE)
        /// <summary>
        /// Enables localized fix notifications that override the dashboard counterparts
        /// </summary>
        /// <param name="title">Fix notification title.</param>
        /// <param name="content">Fix notification content.</param>
        /// <returns></returns>
        public static void SetLocalizedFixNotifications(string title, string content)
        {
            G.LocalizedFixTitle = title;
            G.LocalizedFixText = content;
            G.HasLocalizedFixes = true;
        }

        /// <summary>
        /// Disables localized fix notifications
        /// </summary>
        /// <returns></returns>
        public static void ResetLocalizedFixNotifications()
        {
            G.HasLocalizedFixes = false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if FREE_BIRD
        public static string GetUrl()
        {
            return G.IsProxyActive ? G.PROXY_URL : G.URL;
        }

        public static void SetUrl(string url)
        {
            G.URL = url;
            G.PROXY_URL = url;
        }

        public static string GetEvtUrl()
        {
            return G.IsProxyActive ? G.EVT_PROXY_URL_PRE : G.EVT_URL_PRE;
        }

        public static void SetEvtUrl(string url)
        {
            G.EVT_URL_PRE = url;
            G.EVT_PROXY_URL_PRE = url;
            G.IsEvtUrlPre = false;
        }

        public static void ResetUrls()
        {
            G.URL = G.DEFAULT_URL;
            G.PROXY_URL = G.DEFAULT_PROXY_URL;
            G.EVT_URL_PRE = G.DEFAULT_EVT_URL_PRE;
            G.EVT_PROXY_URL_PRE = G.DEFAULT_EVT_PROXY_URL_PRE;
            G.IsEvtUrlPre = G.DEFAULT_IS_EVT_URL_PRE;
        }

        public void SendPingEvent()
        {
            HandleEvent(G.PingEvent, false, true);
        }
#endif //FREE_BIRD

        #endregion

        #region [ Private Core Methods ]

        private void OnBugSenseUnhandledException(BugSenseUnhandledExceptionEventArgs e)
        {
            EventHandler<BugSenseUnhandledExceptionEventArgs> handler = UnhandledException;
            if (handler != null)
                handler(this, e);
        }

#if WINDOWS_PHONE
        private void OnUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs args)
#else
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
#endif
        {
#if WINDOWS_PHONE
            var except = args.ExceptionObject;
            string excMsg = "";
#elif NETFX_CORE
            var except = args.Exception;
            string excMsg = args.Message;
#else
			var except = args.ExceptionObject;
			string excMsg = "";
#endif
            if (except is BugSenseUnhandledException)
                return;

#if NETFX_CORE
            TryToSetDispatcher();
#endif
#if (WINDOWS_PHONE || NETFX_CORE)
            bool handled = args.Handled;
            args.Handled = true;
            var e = new BugSenseUnhandledExceptionEventArgs(except, args.Handled);
            OnBugSenseUnhandledException(e);
            if (e.Cancel)
                return;
            //NOTE: debugger section
            if (Debugger.IsAttached)
                if(_options == null || !_options.HandleWhileDebugging)
                    return;
            Handle(except, excMsg, false, e.Comment, _options, !handled);
            args.Handled = true;
#else
			var e = new BugSenseUnhandledExceptionEventArgs((Exception)except, false);
			OnBugSenseUnhandledException(e);
			if (e.Cancel)
				return;
			//NOTE: debugger section
            if (Debugger.IsAttached)
                if(_options == null || !_options.HandleWhileDebugging)
                    return;
			Handle((Exception)except, excMsg, false, e.Comment, _options, true);
#endif
        }

        private DateTime _lastMethodHandledCalledAt;
        private DateTime _lastFlatCalledAt;

        private void HandleEvent(string tag, bool cacheonly, bool reserved)
        {
            if (DateTime.Now.AddSeconds(-1) < _lastFlatCalledAt)
            {
                return;
            }
            _lastFlatCalledAt = DateTime.Now;

            if (!cacheonly)
                _worker.SendEventNow(GetEnvironment(true), tag, reserved);
            else
                _worker.CacheEvent(GetEnvironment(true), tag, reserved);
        }

        private void Handle(Exception e, string excMsg, bool fromTask, string tag,
            NotificationOptions options, bool isFatal, Dictionary<string, string> logExtra = null)
        {
            if (DateTime.Now.AddSeconds(-1) < _lastMethodHandledCalledAt)
            {
                return;
            }
            _lastMethodHandledCalledAt = DateTime.Now;

            //NOTE: debugger section
            if (Debugger.IsAttached)
                if (_options == null || !_options.HandleWhileDebugging)
                {
                    // Don't send the error
                    return;
                }
            bool handled = !isFatal;
#if WINDOWS_PHONE
            bool basicEnv = false;
#else
            bool basicEnv = fromTask;
#endif
            var request = new BugSenseExceptionRequest(e.ToBugSenseEx(excMsg, handled, tag, GetBreadcrumbs()),
                GetEnvironment(basicEnv), GetAllExtraData(logExtra));

            if (handled)
            {
#if WINDOWS_PHONE
                LogAndSend(request, true, false);
#else
                LogAndSend(request, false, false);
#endif
                return;
            }

            try
            {
#if (WINDOWS_PHONE || NETFX_CORE)
				switch (options.Type)
                {
                    case enNotificationType.MessageBoxConfirm:
                        if (!NotificationBox.IsOpen())
                        {
                            try
                            {
                                if (!NotificationBox.IsOpen())
                                    NotificationBox.Show(Labels.ConfirmationTitle, Labels.ConfirmationText,
                                        new NotificationBoxCommand(Labels.OkMessage, () =>
                                            {
                                                LogAndSend(request, false, isFatal);
                                            }),
                                        new NotificationBoxCommand(Labels.CancelMessage, () =>
                                            {
                                                if (isFatal)
                                                    _worker.Die(_actOnExit);
                                            }));
                            }
                            catch (BugSenseUnhandledException ex)
                            {
                                throw ex;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        break;
                    default:
                        LogAndSend(request, false, isFatal);
                        break;
                }
#else
				LogAndSend(request, false, isFatal);
#endif
            }
            catch (BugSenseUnhandledException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
#if (WINDOWS_PHONE || NETFX_CORE)
				if (options.Type != enNotificationType.MessageBoxConfirm)
                    LogAndSend(request, false, isFatal);
#else
				LogAndSend(request, false, isFatal);
#endif
            }
        }

        private void LogAndSend (BugSenseExceptionRequest request, bool cacheOnly, bool isFatal)
		{
			if (cacheOnly)
				_worker.CacheError (request, isFatal);
			else {
#if DEBUG
				_worker.SendErrorNow (request, isFatal, DebugFixResponse);
#else
				_worker.SendErrorNow (request, isFatal);
#endif

#if (!WINDOWS_PHONE && !NETFX_CORE)
				if(isFatal)
					Helpers.SleepFor(4000);
#endif
			}
        }

        private void FixNotifier(object sender, FixNotificationEventArgs e)
        {
            if (e != null)
            {
#if WINDOWS_PHONE
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        _worker.NotificationHelper(e.FixResponse, e.IsFatal, _actOnExit);
                    });
#elif NETFX_CORE
                TryToSetDispatcher();
                if (_dispatcher != null)
                    Task.Run(async () =>
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    _worker.NotificationHelper(e.FixResponse, e.IsFatal, _actOnExit);
                                });
                        });
#else
				_worker.NotificationHelper(e.FixResponse, e.IsFatal, _actOnExit);
#endif
            }
        }

        #endregion

        #region [ Private Helper Methods ]

#if NETFX_CORE
        private void TryToSetDispatcher()
        {
            if (_dispatcher == null)
            {
                try
                {
                    _dispatcher = Application.Current.Resources.Dispatcher;
                }
                catch (Exception)
                {
                    try
                    {
                        _dispatcher = Window.Current.Dispatcher;
                    }
                    catch (Exception)
                    {
                        _dispatcher = null;
                    }
                }
            }
        }
#endif

        private Dictionary<string, string> GetAllExtraData(Dictionary<string, string> loggedExcExtra = null)
        {
			Dictionary<string, string> res = new Dictionary<string, string> ();
			string r1 = "";
			
			lock (G.LockThis) {
				res = new Dictionary<string, string> (G.ExcExtraData.Dict);
				r1 = G.Timer.ElapsedMilliseconds.ToString ();
			}
			if (loggedExcExtra != null) {
				ExtraData le = new ExtraData (loggedExcExtra);
				var pairs = le.Dict;
				if (pairs != null)
					foreach (var pair in pairs)
						res [pair.Key] = pair.Value;
			}
			res ["ms_from_start"] = r1;
			
			return res;
		}

        private AppEnvironment GetEnvironment(bool basic = false)
        {
            return BugSenseEnvironment.GetEnvironment(_appName, _appVersion, _worker.ManageUUID(), basic);
        }

        #endregion

    }
}
