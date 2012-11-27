using System;
using System.Collections.Generic;
using System.Net;
#if WINDOWS_PHONE
using System.Net.Browser;
#elif NETFX_CORE
using System.Net.Http;
#endif
using System.IO;
using System.Threading.Tasks;

namespace BugSense.Extensions
{
    internal class WebRequests
    {
        public static void DoHttpPost(string URL,
            Dictionary<string, string> headers, string content_type, string contents,
            string cached_request_fname,
            Action<string> Act_On_Response)
        {
#if WINDOWS_PHONE
            string data = HttpUtility.UrlEncode(contents);
            WebRequest request = WebRequestCreator.ClientHttp.Create(new Uri(URL));
            request.Headers[HttpRequestHeader.UserAgent] = "WP8";
#elif NETFX_CORE
            var data = new StringContent(contents);
            WebRequest request = WebRequest.Create(URL);
#endif

            request.Method = "POST";
            request.ContentType = content_type;
            if (headers != null)
                foreach (var pair in headers)
                    request.Headers[pair.Key] = pair.Value;

            string contextFilePath = cached_request_fname;
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
                        WebResponse resp = null;
                        try
                        {
                            resp = request.EndGetResponse(a);
                            if (!string.IsNullOrEmpty(contextFilePath))
                            {
                                // Request sent! Delete it!
                                Task.Run(async () => await Files.Delete(contextFilePath)).Wait();
                            }
                        }
                        catch (Exception)
                        {
                            resp = null;
                        }

                        if (Act_On_Response != null)
                            ProcessResponse(resp, Act_On_Response);
                    }, null);
                }
                catch (Exception)
                {
                }
            }, contents);
        }

        private static void ProcessResponse(WebResponse response, Action<string> Act_On_Response)
        {
            string string_response = "";

            if (response != null)
            {
                try
                {
                    byte[] bytes_resp = new byte[response.ContentLength + 10];
                    response.GetResponseStream().Read(bytes_resp, 0, (int)response.ContentLength);
                    string_response = System.Text.UTF8Encoding.UTF8.GetString(bytes_resp, 0, (int)response.ContentLength);

                    //NOTE: for testing purposes
                    
                    string_response = "{\"data\": {" +
                        //"\"url\": \"http://www.bugsense.com\", " +
                        "\"url\": \"ms-windows-store:PDP?PFN=microsoft.microsoftskydrive_8wekyb3d8bbwe\", " +
                        "\"contentText\": \"This error has been fixed in the latest release\", " +
                        "\"contentTitle\": \"Please update!\"" +
                        "}, " +
                        "\"error\": null}";
                    
                }
                catch (Exception)
                {
                }
            }

            if (Act_On_Response != null)
                Act_On_Response(string_response);
        }
    }
}
