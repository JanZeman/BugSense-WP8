using BugSense.Internal;
using System;
using System.Collections.Generic;
using System.Net;
#if WINDOWS_PHONE
using System.Net.Browser;
#else
using System.Net.Http;
#endif
using System.IO;
using System.Threading.Tasks;

namespace BugSense.Extensions
{
    internal class WebRequests
    {
        #region [ Public methods ]
        public static string GetExceptionURL()
        {
            return G.IsProxyActive ? G.PROXY_URL : G.URL;
        }

        public static string GetEventURL(string uuid)
        {
            string urlprime = G.IsProxyActive ? G.EVT_PROXY_URL_PRE : G.EVT_URL_PRE;
            string url = G.IsEvtUrlPre ?
                urlprime + G.API_KEY + "/" + uuid :
                urlprime;

            return url;
        }

        public static void DoHttpPost(string URL,
            Dictionary<string, string> headers, string contentType, string contents,
            string cachedRequestFname,
            Action<string> actOnResponse, bool overrideResponse = false)
        {
#if WINDOWS_PHONE
            WebRequest request = WebRequestCreator.ClientHttp.Create(new Uri(URL));
            request.Headers[HttpRequestHeader.UserAgent] = "WP8";

            request.Method = "POST";
            request.ContentType = contentType;
            if (headers != null)
                foreach (var pair in headers)
                    request.Headers[pair.Key] = pair.Value;

            string contextFilePath = cachedRequestFname;
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

                        if (actOnResponse != null)
                        {
                            try
                            {
                                byte[] bytes_resp = new byte[resp.ContentLength + 10];
                                resp.GetResponseStream().Read(bytes_resp, 0, (int)resp.ContentLength);
                                string string_response = System.Text.UTF8Encoding.UTF8.GetString(
                                    bytes_resp, 0, (int)resp.ContentLength);

                                ProcessResponse(string_response, actOnResponse, overrideResponse);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }, null);
                }
                catch (Exception)
                {
                }
            }, contents);
#else
            string content = "";
            string contextFilePath = cachedRequestFname;
            try
            {
                HttpClient client = new HttpClient();
                if (headers != null)
                    foreach (var pair in headers)
                        client.DefaultRequestHeaders.Add(pair.Key, pair.Value);
                HttpResponseMessage response = null;
                StringContent sc = new StringContent(contents, System.Text.Encoding.UTF8, contentType);
                Task.Run(async () => {
					response = await client.PostAsync(URL, sc);
				}).Wait();
                response.EnsureSuccessStatusCode();
                Task.Run(async () => {
					content = await response.Content.ReadAsStringAsync();
				}).Wait();
                if (!string.IsNullOrEmpty(contextFilePath))
                {
                    // Request sent! Delete it!
                    Task.Run(async () => await Files.Delete(contextFilePath)).Wait();
                }
                client.Dispose();
                if (actOnResponse != null)
                    ProcessResponse(content, actOnResponse, overrideResponse);
            }
            catch (HttpRequestException e1)
            {
                Helpers.Log("WebRequests: Http Exception with Message :" + e1.Message);
            }
            catch (Exception e2)
            {
                Helpers.Log("WebRequests: Exception with Message :" + e2.Message);
            }
#endif
        }
        #endregion

        #region [ Private methods ]
        private static void ProcessResponse(string response, Action<string> actOnResponse,
		                                    bool overrideResponse = false)
        {
            if (!String.IsNullOrEmpty(response))
            {
                try
                {
                    //NOTE: for testing purposes
                    if(overrideResponse)
	                    response = "{\"data\": {" +
#if (WINDOWS_PHONE || NETFX_CORE)
	                        "\"url\": \"ms-windows-store:PDP?PFN=microsoft.microsoftskydrive_8wekyb3d8bbwe\", " +
#else
							"\"url\": \"http://www.bugsense.com\", " +
#endif
	                        "\"contentText\": \"This error has been fixed in the latest release\", " +
	                        "\"contentTitle\": \"Please update!\"" +
	                        "}, " +
	                        "\"error\": null}";
                }
                catch (Exception)
                {
                }
            }

			if (actOnResponse != null)
				actOnResponse(response);
        }
        #endregion
    }
}
