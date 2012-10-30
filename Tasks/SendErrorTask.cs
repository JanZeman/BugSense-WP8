using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Browser;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BugSense.Coroutines;
using BugSense.Internal;
using BugSense.Notifications;

namespace BugSense.Tasks {
    internal class SendErrorTask : IResult {

        private string _filePath;

        public SendErrorTask() { }
        public SendErrorTask(string filePath) { _filePath = filePath; }

        public void Execute(ActionExecutionContext context)
        {
            if (string.IsNullOrEmpty(_filePath)) {
                string fileName = context["fileName"] as string;
                if (string.IsNullOrEmpty(fileName))
                    return;
                _filePath = G.FolderName + "\\" + fileName;
            }

            using (var storage = IsolatedStorageFile.GetUserStoreForApplication()) {
                if (storage.DirectoryExists(G.FolderName)) {
                    if (!storage.FileExists(_filePath))
                        return;
                    using (var fileStream = storage.OpenFile(_filePath, FileMode.Open)) {
                        using (StreamReader sr = new StreamReader(fileStream)) {
                            string data = sr.ReadToEnd();
                            ExecuteRequest(data);
                        }
                    }
                }
            }
        }

        public event EventHandler<ResultCompletionEventArgs> Completed;

        public void TaskCompleted()
        {
            EventHandler<ResultCompletionEventArgs> handler = Completed;
            if (handler != null) handler(this, new ResultCompletionEventArgs());
        }

        private void CacheResponse(WebResponse response)
        {
            byte[] bytes_resp = new byte[response.ContentLength + 10];
            response.GetResponseStream().Read(bytes_resp, 0, (int)response.ContentLength);
            string string_response = System.Text.UTF8Encoding.UTF8.GetString(bytes_resp, 0, (int)response.ContentLength);

            // NOTE: for testing purposes
            //string_response = "{\"data\": {\"url\": \"http://www.bugsense.com\", \"contentText\": \"description\", \"contentTitle\": \"Title\"}, \"error\": null}";

            if (string_response.IndexOf("contentTitle") > 0)
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (storage.DirectoryExists(G.FolderName))
                    {
                        using (var fileStream = storage.OpenFile(G.FIX_TMP_FILE, FileMode.OpenOrCreate))
                        {
                            using (StreamWriter sr = new StreamWriter(fileStream))
                            {
                                sr.Write(string_response);
                            }
                        }
                    }
                }
            }
        }

        private void ExecuteRequest(string errorJson)
        {
            try {
                errorJson = "data=" + HttpUtility.UrlEncode(errorJson);
                var request = WebRequestCreator.ClientHttp.Create(new Uri(G.URL));
                
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers[HttpRequestHeader.UserAgent] = "WP8";
                request.Headers["X-BugSense-Api-Key"] = G.API_KEY;

                string contextFilePath = _filePath;
                request.BeginGetRequestStream(ar => {
                    try {
                        var requestStream = request.EndGetRequestStream(ar);
                        using (var sw = new StreamWriter(requestStream)) {
                            sw.Write(ar.AsyncState);
                        }
                        request.BeginGetResponse(a => {
                            try {
                                //Error sent! Delete it!
                                using (var storage = IsolatedStorageFile.GetUserStoreForApplication()) {
                                    if (storage.FileExists(contextFilePath))
                                        storage.DeleteFile(contextFilePath);
                                }
                            }
                            catch { }
                            finally {
                                WebResponse resp = request.EndGetResponse(a);
                                CacheResponse(resp);
                                TaskCompleted();
                            }
                        }, null);
                       
                    }
                    catch {
                        TaskCompleted();
                    }
                }, errorJson);
            }
            catch { /* Error is already saved so next time the app starts will try to send it again*/
                TaskCompleted();
            }
        }
    }
}
