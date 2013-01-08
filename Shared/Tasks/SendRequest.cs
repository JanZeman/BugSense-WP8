using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class SendRequest
    {
		#region [ Attributes ]
        private string _URL = WebRequests.GetExceptionURL();
        private string _fileName;
        private bool _isJSON = true;
        private Action<string> _actOnResponse = null;
		private bool _debugFixResponse = false;
		#endregion

		#region [ Ctor ]
        public SendRequest(string fileName)
        {
            _fileName = fileName;
			_debugFixResponse = false;
        }

        public SendRequest(string fileName, bool isJSON)
        {
            _fileName = fileName;
            _debugFixResponse = false;
            _isJSON = isJSON;
        }
        
        public SendRequest(string URL, string fileName, bool isJSON)
        {
            _URL = URL;
            _fileName = fileName;
            _isJSON = isJSON;
			_debugFixResponse = false;
        }

		public SendRequest(string fileName, Action<string> actOnResponse)
        {
            _fileName = fileName;
            _actOnResponse = actOnResponse;
			_debugFixResponse = false;
        }

		public SendRequest(string URL, string fileName, bool isJSON, Action<string> actOnResponse)
        {
            _URL = URL;
            _fileName = fileName;
            _isJSON = isJSON;
            _actOnResponse = actOnResponse;
			_debugFixResponse = false;
        }
		#endregion

		#region [ Public methods ]
		public void setDebugFixedResponse (bool debugFixResponse)
		{
			_debugFixResponse = debugFixResponse;
		}

        public async Task<bool> Execute()
        {
            string fname = _fileName;

            if (string.IsNullOrEmpty(fname))
                return false;

            fname = Path.Combine(G.FolderName, _fileName);

			Helpers.Log("SendRequest 1/2 :: Reading File " + _fileName);
            Tuple<string, bool> res = await Files.ReadFrom(fname);
            if (!res.Item2)
            {
				Helpers.Log("SendRequest 1/2 :: Error reading File " + _fileName);
                return false;
            }
			Helpers.Log("SendRequest 1/2 :: Done reading File " + _fileName);

            return ExecuteRequest(_URL, res.Item1, fname);
        }
		#endregion

		#region [ Private methods ]
        private bool ExecuteRequest(string URL, string msg, string cached_fname)
        {
            try
            {
                if (_isJSON)
                {
					Helpers.Log("SendRequest 2/2 :: Preparing JSON");
                    msg = "data=" + Uri.EscapeDataString(msg);
                }
            }
            catch (Exception)
            {
                return false;
            }

			Helpers.Log("SendRequest 2/2 :: Doing HttpPost");
			if (_debugFixResponse)
				Helpers.Log("SendRequest 2/2 :: Fixed Response for debugging");
            WebRequests.DoHttpPost(URL,
                new Dictionary<string, string>() { { "X-BugSense-Api-Key", G.API_KEY } },
                _isJSON ? "application/x-www-form-urlencoded" : "text/plain",
                msg, cached_fname, _actOnResponse, _debugFixResponse);
			Helpers.Log("SendRequest 2/2 :: Done HttpPost");

            return true;
        }
		#endregion
    }
}
