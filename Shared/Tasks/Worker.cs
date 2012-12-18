using BugSense.Extensions;
using BugSense.Internal;
#if WINDOWS_PHONE
using BugSense.InternalWP8;
using BugSense_WP8;
#elif NETFX_CORE
using BugSense.InternalW8;
using BugSense_W8;
#endif
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
#endif

namespace BugSense.Tasks
{
	#region [ Signal handlers ]
    internal delegate void FixNotificationEventHandler(object sender, FixNotificationEventArgs e);
	#endregion

    internal sealed class Worker
    {
		#region [ Attributes ]
        public event FixNotificationEventHandler FixNotification;

        private readonly Semaphore _lockToSend = new Semaphore(1, 1);
        private readonly Semaphore _lockToSendEvt = new Semaphore(1, 1);
        private string _prevEvtTs = "";
        private static readonly DataContractJsonSerializer _jsonDeserializer =
            new DataContractJsonSerializer(typeof(FixResponse));
		#endregion

		#region [ Ctor ]
        public Worker()
        {
        }

		public Worker(bool with)
        {
            if (with)
                ProcessAll();
        }
		#endregion

		#region [ Public methods ]
        public void ProcessAll()
        {
            Task t = new Task(async () =>
                {
                    string uuid = await UUIDFactory.Get();
                    ProcessRequests msgs = new ProcessRequests(uuid);

                    try
                    {
                        _lockToSend.WaitOne();
                        Helpers.Log("begin(ProcessAll)");
                        int res = await msgs.Execute();
                        Helpers.Log("end(ProcessAll): " + res);
                        _lockToSend.Release();
                    }
                    catch (Exception)
                    {
                        Helpers.Log("fail(ProcessAll)");
                    }
                });
            t.Start();
        }

        public void CacheError(BugSenseExceptionRequest req, bool isFatal)
        {
            Task t = new Task(async () =>
                {
                    LogError err = new LogError(req, isFatal);
                    Helpers.Log("begin(CacheError)");
                    string res = await err.Execute();
                    Helpers.Log("end(CacheError): " + res);
                });
            t.Start();
        }

        public void SendErrorNow(BugSenseExceptionRequest req, bool isFatal,
			bool debugFixResponse = false)
        {
            Task t = new Task(async () =>
                {
                    try
                    {
                        Helpers.Log("begin(SendErrorNow-1)");
                        LogError err = new LogError(req, isFatal);
                        string str = await err.Execute();
                        if (!string.IsNullOrEmpty(str))
                        {
                            try
                            {
                                SendRequest msg = null;
                                if (isFatal)
                                    msg = new SendRequest(str, FixNotificationAndDieAction);
                                else
                                    msg = new SendRequest(str, FixNotificationAction);
								if (debugFixResponse)
									msg.setDebugFixedResponse(true);
                                _lockToSend.WaitOne();
                                Helpers.Log("begin(SendErrorNow-2)");
                                bool res = await msg.Execute();
                                Helpers.Log("end(SendErrorNow-2): " + res);
                                _lockToSend.Release();
                            }
                            catch (Exception)
                            {
                                Helpers.Log("fail(SendErrorNow)");
                            }
                        }
                        Helpers.Log("end(SendErrorNow-1)");
                    }
                    catch (Exception)
                    {
                        Helpers.Log("fail(SendErrorNow)");
                    }
                });
			t.Start();
        }

        public void CacheEvent(AppEnvironment env, string tag, bool reserved = false)
        {
            var evtrequest = new BugSenseEventRequest(env, tag, reserved);
            string contents = evtrequest.getFlatLine();

            Task t = new Task(async () =>
                {
                    try
                    {
                        LogEvent evt = new LogEvent(contents, reserved);
                        _lockToSendEvt.WaitOne();
                        if (!evtrequest.TimeStamp.Equals(_prevEvtTs))
                        {
                            _prevEvtTs = evtrequest.TimeStamp;
                            _lockToSendEvt.Release();
                            Helpers.Log("begin(CacheEvent)");
                            string res = await evt.Execute();
                            Helpers.Log("end(CacheEvent): " + res);
                        }
                        else
                        {
                            Helpers.Log("Duplicate_Event(CacheEvent(" + _prevEvtTs + "))");
                            _lockToSendEvt.Release();
                        }
                    }
                    catch (Exception)
                    {
                        Helpers.Log("fail(CacheEvent)");
                    }
                });
            t.Start();
        }

        public void SendEventNow(AppEnvironment env, string tag, bool reserved = false)
        {
            var evtrequest = new BugSenseEventRequest(env, tag, reserved);
            string contents = evtrequest.getFlatLine();

            Task t = new Task(async () =>
                {
                    try
                    {
                        string uuid = env.Uid;
                        string url = WebRequests.GetEventURL(uuid);
                        _lockToSendEvt.WaitOne();
                        if (!evtrequest.TimeStamp.Equals(_prevEvtTs))
                        {
                            _prevEvtTs = evtrequest.TimeStamp;
                            _lockToSendEvt.Release();
                            Helpers.Log("begin(SendEventNow-1)");
                            LogEvent evt = new LogEvent(contents, reserved);
                            string str = await evt.Execute();
                            if (!string.IsNullOrEmpty(str))
                            {
                                SendRequest msg = new SendRequest(url, str, false);
                                _lockToSend.WaitOne();
                                Helpers.Log("begin(SendEventNow-2)");
                                bool res = await msg.Execute();
                                Helpers.Log("end(SendEventNow-2): " + res);
                                _lockToSend.Release();
                            }
                            Helpers.Log("end(SendEventNow-1)");
                        }
                        else
                        {
                            Helpers.Log("Duplicate_Event(SendEventNow(" + _prevEvtTs + "))");
                            _lockToSendEvt.Release();
                        }
                    }
                    catch (Exception)
                    {
                        Helpers.Log("fail(SendEventNow)");
                    }
                });
            t.Start();
        }

        public string ManageUUID ()
		{
			string uuid = G.UUID;

			if (string.IsNullOrEmpty (uuid))
				Task.Run(async () => {
					uuid = await UUIDFactory.Get();
				}).Wait();

            return uuid;
        }

		public void NotificationHelper(string response, bool isFatal, Action act)
        {
            if (response.IndexOf("contentTitle") > 0)
            {
                try
                {
                    Helpers.Log("init(NotificationHelper)");
                    byte[] byteArray = Encoding.UTF8.GetBytes(response);
                    using (MemoryStream ms = new MemoryStream(byteArray))
                    {
                        FixResponse fixer = (FixResponse)_jsonDeserializer.ReadObject(ms);
#if (WINDOWS_PHONE || NETFX_CORE)
                        string title = G.HasLocalizedFixes ? G.LocalizedFixTitle : fixer.Data.ContentTitle;
                        string text = G.HasLocalizedFixes ? G.LocalizedFixText : fixer.Data.ContentText;
						if (!NotificationBox.IsOpen())
                            NotificationBox.Show(title, text,
                            new NotificationBoxCommand(Labels.UpdateMessage, () =>
                            {
                                // go to update
                                Helpers.Log("update(NotificationHelper)");
                                Browsers.Goto(fixer.Data.Url);
                                if (isFatal && act != null)
                                    act();
                            }),
                            new NotificationBoxCommand(Labels.CancelMessage, () =>
                            {
                                // return to app
                                Helpers.Log("cancel(NotificationHelper)");
                                if (isFatal)
                                    Die(act);
                            }));
#else
						// go to update
						Helpers.Log("update(NotificationHelper)");
						Browsers.Goto(fixer.Data.Url);
						if (isFatal && act != null)
							act();
#endif
                    }
				}
                catch (BugSenseUnhandledException ex)
                {
                    throw ex;
                }
                catch (Exception)
                {
                    Helpers.Log("fail(NotificationHelper)");
                }
            }
            else if (isFatal)
                Die(act);
        }

        public void Die(Action act)
        {
            if (act != null)
                act();
#if WINDOWS_PHONE
            throw new BugSenseUnhandledException();
#elif NETFX_CORE
            Application.Current.Exit();
#else
			throw new BugSenseUnhandledException();
#endif
        }
		#endregion

		#region [ Private methods ]
        private void FixNotificationAction(string response)
        {
            if (FixNotification != null)
                FixNotification(this, new FixNotificationEventArgs(response, false));
        }

        private void FixNotificationAndDieAction(string response)
        {
            if (FixNotification != null)
                FixNotification(this, new FixNotificationEventArgs(response, true));
        }
		#endregion
    }
}
