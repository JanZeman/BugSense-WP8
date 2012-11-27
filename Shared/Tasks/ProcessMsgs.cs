using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class ProcessMsgs
    {
        private string _uuid = "";

        public ProcessMsgs(string uuid)
        {
            _uuid = uuid;
        }

        public async Task<int> Execute()
        {
            int r = 0;

            try
            {
                var taskList = new List<SendMsg>();
                int counter = 0;

                List<string> list = await Files.GetDirFilenames(G.FolderName, G.CrashFileNamePrefix);
                List<string> list2 = await Files.GetDirFilenames(G.FolderName, G.LoggedExceptionFileNamePrefix);
                List<string> list3 = await Files.GetDirFilenames(G.FolderName, G.PingFileNamePrefix);

                counter = await ProcessList(taskList, list, G.MaxCrashes, true, _uuid);
                Helpers.Log("ProcessMsg 1/4 :: gotExceptions: " + counter);
                counter = await ProcessList(taskList, list2, G.MaxLoggedExceptions, true, _uuid);
                Helpers.Log("ProcessMsg 2/4 :: gotEvts: " + counter);
                counter = await ProcessList(taskList, list3, G.MaxPings, false, _uuid);
                Helpers.Log("ProcessMsg 2/4 :: gotEvts: " + counter);

                Helpers.Log("ProcessMsg 4/4 :: sending: " + taskList.Count);
                if (taskList != null)
                {
                    foreach (SendMsg err in taskList)
                    {
                        //NOTE: r is equal to the _attempted_ executions
                        //  (we don't know if each web request was successful)
                        bool rr = await err.Execute();
                        if (rr)
                            r++;
                    }
                }
            }
            catch (Exception)
            { // Swallow like a fish - Not much that we can do here
            }

            return r;
        }

        private async static Task<int> ProcessList(List<SendMsg> taskList, List<string> list, int max,
            bool isException, string uuid)
        {
            int counter = 0;

            if (list != null)
            {
                foreach (var fileName in list)
                {
                    if (string.IsNullOrEmpty(fileName))
                        continue;
                    // If there are more messages in the pool we just delete them
                    if (counter < max)
                    {
                        if (isException)
                            taskList.Add(new SendMsg(fileName));
                        else
                        {
                            string url = G.IsEvtUrlPre ?
                                G.EVT_URL_PRE + G.API_KEY + "/" + uuid :
                                G.EVT_URL_PRE;
                            taskList.Add(new SendMsg(url, fileName, false));
                        }
                    }
                    else
                        await Files.Delete(G.FolderName, fileName);
                    counter++;
                    //
                }
            }

            return counter;
        }
    }
}
