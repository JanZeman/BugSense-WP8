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
    internal delegate void FixNotificationEventHandler(object sender, FixNotificationEventArgs e);

    internal sealed class Worker
    {
        public event FixNotificationEventHandler FixNotification;

        private readonly Semaphore _lockToSend = new Semaphore(1, 1);
        private readonly Semaphore _lockToSendEvt = new Semaphore(1, 1);
        private string _prevEvtTs = "";
        private static readonly DataContractJsonSerializer _jsonDeserializer =
            new DataContractJsonSerializer(typeof(FixResponse));

        public Worker()
        {
        }
        public Worker(bool with)
        {
            if (with)
                ProcessAll();
        }

        public void ProcessAll()
        {
            string uuid = ManageUUID();
            ProcessMsgs msgs = new ProcessMsgs(uuid);
            Task t = new Task(async () =>
                {
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

        public void CacheError(BugSenseRequest req, bool isFatal)
        {
            LogError err = new LogError(req, isFatal);
            Task t = new Task(async () =>
                {
                    Helpers.Log("begin(CacheError)");
                    string res = await err.Execute();
                    Helpers.Log("end(CacheError): " + res);
                });
            t.Start();
        }

        public void SendErrorNow(BugSenseRequest req, bool isFatal)
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
                                SendMsg msg = null;
                                if (isFatal)
                                    msg = new SendMsg(str, FixNotificationAndDieAction);
                                else
                                    msg = new SendMsg(str, FixNotificationAction);
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

        public void CacheEvent(AppEnvironment env, string tag)
        {
            var evtrequest = new BugSenseEvent(env, tag);
            string contents = evtrequest.getFlatLine();

            LogEvent evt = new LogEvent(contents);
            Task t = new Task(async () =>
                {
                    try
                    {
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

        public void SendEventNow(AppEnvironment env, string tag)
        {
            var evtrequest = new BugSenseEvent(env, tag);
            string contents = evtrequest.getFlatLine();

            string uuid = ManageUUID();
            string url = G.IsEvtUrlPre ?
                G.EVT_URL_PRE + G.API_KEY + "/" + uuid :
                G.EVT_URL_PRE;

            Task t = new Task(async () =>
                {
                    try
                    {
                        _lockToSendEvt.WaitOne();
                        if (!evtrequest.TimeStamp.Equals(_prevEvtTs))
                        {
                            _prevEvtTs = evtrequest.TimeStamp;
                            _lockToSendEvt.Release();
                            Helpers.Log("begin(SendEventNow-1)");
                            LogEvent evt = new LogEvent(contents);
                            string str = await evt.Execute();
                            if (!string.IsNullOrEmpty(str))
                            {
                                SendMsg msg = new SendMsg(url, str, false);
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

        public string ManageUUID()
        {
            string uuid = G.UUID;

            if (string.IsNullOrEmpty(uuid))
                Task.Run(async () => uuid = await UUIDFactory.Get()).Wait();

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
                        if (!NotificationBox.IsOpen())
                            NotificationBox.Show(fixer.Data.ContentTitle, fixer.Data.ContentText,
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
#endif
        }

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
    }
}
