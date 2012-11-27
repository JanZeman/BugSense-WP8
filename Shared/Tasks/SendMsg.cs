using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class SendMsg
    {
        private string _URL = G.URL;
        private string _fileName;
        private bool _isJSON = true;
        private Action<string> _actOnResponse = null;

        public SendMsg(string fileName)
        {
            _fileName = fileName;
        }
        public SendMsg(string URL, string fileName, bool isJSON)
        {
            _URL = URL;
            _fileName = fileName;
            _isJSON = isJSON;
        }
        public SendMsg(string fileName, Action<string> actOnResponse)
        {
            _fileName = fileName;
            _actOnResponse = actOnResponse;
        }
        public SendMsg(string URL, string fileName, bool isJSON, Action<string> actOnResponse)
        {
            _URL = URL;
            _fileName = fileName;
            _isJSON = isJSON;
            _actOnResponse = actOnResponse;
        }

        public async Task<bool> Execute()
        {
            string fname = _fileName;

            if (string.IsNullOrEmpty(fname))
                return false;

            fname = Path.Combine(G.FolderName, _fileName);

            Helpers.Log("SendMsg 1/2 :: Reading File " + _fileName);
            FilesReadResult res = await Files.ReadFrom(fname);
            if (!res.Result)
            {
                Helpers.Log("SendMsg 1/2 :: Error reading File " + _fileName);
                return false;
            }
            Helpers.Log("SendMsg 1/2 :: Done reading File " + _fileName);

            return ExecuteRequest(_URL, res.Str, fname);
        }

        private bool ExecuteRequest(string URL, string msg, string cached_fname)
        {
            try
            {
                if (_isJSON)
                {
                    Helpers.Log("SendMsg 2/2 :: Preparing JSON");
                    msg = "data=" + Uri.EscapeDataString(msg);
                }
            }
            catch (Exception)
            {
                return false;
            }

            Helpers.Log("SendMsg 2/2 :: Doing HttpPost");
            WebRequests.DoHttpPost(URL,
                new Dictionary<string, string>() { { "X-BugSense-Api-Key", G.API_KEY } },
                _isJSON ? "application/x-www-form-urlencoded" : "text/plain",
                msg, cached_fname, _actOnResponse);
            Helpers.Log("SendMsg 2/2 :: Done HttpPost ");

            return true;
        }
    }
}
