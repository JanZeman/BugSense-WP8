using BugSense.Internal;

#if WINDOWS_PHONE
using BugSense.InternalWP8;
#elif NETFX_CORE
using BugSense.InternalW8;
#else
using BugSense.InternalDotNet;
#endif
using BugSense.Extensions;
using BugSense.Tasks;
using EntropyUUID;

#if __MonoCS__
using NUnit.Framework;
#elif WINDOWS_PHONE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BugSense.UnitTests
{
#if __MonoCS__
	[TestFixture()]
	#else
	[TestClass]
#endif
	public class TestTasks1
	{
		private Tuple<int, bool> GetCachedTest (string prefix, string fname)
		{
			int n = 0;
			bool e = false;

			Task.Run (async () => {
				List<string> x = await Files.GetDirFilenames ("BugSense_Data", prefix);
				if (!String.IsNullOrEmpty (fname))
					e = await Files.Exists (Path.Combine ("BugSense_Data", fname));
				n = x.Count;
			}).Wait ();

			return new Tuple<int, bool> (n, e);
		}

		private string CacheTestCrash (string uuid)
		{
			string res = "";

			Exception exc1 = new Exception ("Test exception");
			var request = new BugSenseExceptionRequest (exc1.ToBugSenseEx (null, false, "", "_ping|ev1|ev2"),
				BugSenseEnvironment.GetEnvironment ("test", "0.0.0.0", uuid, true),
				new Dictionary<string, string> () {
					{ "k1", "val1"},
					{ "k2", "val2"},
					{ "k3", "val3"}
			});

			LogError err = new LogError (request, true);
			Task.Run (async () => {
				res = await err.Execute ();
			}).Wait ();

			return res;
		}

		private string CacheTestHandledException (string uuid)
		{
			string res = "";
			
			Exception exc1 = new Exception ("Test exception");
			var request = new BugSenseExceptionRequest (exc1.ToBugSenseEx (null, true, "", "_ping|ev1|ev2"),
				BugSenseEnvironment.GetEnvironment ("test", "0.0.0.0", uuid, true),
				new Dictionary<string, string> () {
					{ "k1", "val1"},
					{ "k2", "val2"},
					{ "k3", "val3"}
			});
			
			LogError err = new LogError (request, false);
			Task.Run (async () => {
				res = await err.Execute ();
			}).Wait ();
			
			return res;
		}

		private string CacheTestPing (string uuid)
		{
			string res = "";

			var evtrequest = new BugSenseEventRequest (
				BugSenseEnvironment.GetEnvironment ("test", "0.0.0.0", uuid, true),
				"_ping", true);

			string contents = evtrequest.getFlatLine ();
			LogEvent evt = new LogEvent (contents, true);
			Task.Run (async () => {
				res = await evt.Execute ();
			}).Wait ();

			return res;
		}

		private string CacheTestEvent (string uuid)
		{
			string res = "";

			var evtrequest = new BugSenseEventRequest (
                BugSenseEnvironment.GetEnvironment ("test", "0.0.0.0", uuid, true),
                "Event");

			string contents = evtrequest.getFlatLine ();
			LogEvent evt = new LogEvent (contents, false);
			Task.Run (async () =>
			{
				res = await evt.Execute ();
			}).Wait ();

			return res;
		}
        
		private bool SendTest (string fname, bool isJSON = true)
		{
			bool res = false;
			
			SendRequest send = new SendRequest (fname, isJSON);
			Task.Run (async () => {
				res = await send.Execute ();
			}).Wait ();
			
			return res;
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestLogCrash ()
		{
			Tuple<int, bool> b = GetCachedTest ("CCC", "");

			string uuid = UUID.GetNew ();
			string res = CacheTestCrash (uuid);

			Tuple<int, bool> a = GetCachedTest ("CCC", res);

			Assert.AreEqual (b.Item1 + 1, a.Item1, "LogCrash file created");
			Assert.AreEqual (true, a.Item2, "LogCrash file exists");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestLogHandledException ()
		{
			Tuple<int, bool> b = GetCachedTest ("LCC", "");
			
			string uuid = UUID.GetNew ();
			string res = CacheTestHandledException (uuid);
			
			Tuple<int, bool> a = GetCachedTest ("LCC", res);
			
			Assert.AreEqual (b.Item1 + 1, a.Item1, "LogHandledException file created");
			Assert.AreEqual (true, a.Item2, "LogHandledException file exists");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestLogPing ()
		{
			Tuple<int, bool> b = GetCachedTest ("PCC", "");

			string uuid = UUID.GetNew ();
			string res = CacheTestPing (uuid);

			Tuple<int, bool> a = GetCachedTest ("PCC", res);

			Assert.AreEqual (b.Item1 + 1, a.Item1, "LogPing file created");
			Assert.AreEqual (true, a.Item2, "LogPing file exists");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
        public void TestLogEvent ()
		{
			Tuple<int, bool> b = GetCachedTest ("ECC", "");

			string uuid = UUID.GetNew ();
			string res = CacheTestEvent (uuid);

			Tuple<int, bool> a = GetCachedTest ("ECC", res);

			Assert.AreEqual (b.Item1 + 1, a.Item1, "LogEvent file created");
			Assert.AreEqual (true, a.Item2, "LogEvent file exists");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestSendCrash ()
		{
			Tuple<int, bool> b = GetCachedTest ("CCC", "");

			string uuid = UUID.GetNew ();
			string res = CacheTestCrash (uuid);
			bool res2 = SendTest (res);
			Helpers.SleepFor (3000);

			Tuple<int, bool> a = GetCachedTest ("CCC", res);
			
			Assert.AreEqual (b.Item1, a.Item1, "SendCrash file created and deleted (network involved)");
			Assert.AreEqual (false, a.Item2, "SendCrash file doesn't exists (network involved)");
			Assert.AreEqual (true, res2, "SendCrash success (network involved)");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestSendHandledException ()
		{
			Tuple<int, bool> b = GetCachedTest ("LCC", "");
			
			string uuid = UUID.GetNew ();
			string res = CacheTestHandledException (uuid);
			bool res2 = SendTest (res);
			Helpers.SleepFor (3000);

			Tuple<int, bool> a = GetCachedTest ("LCC", res);
			
			Assert.AreEqual (b.Item1, a.Item1, "SendHandledException file created and deleted (network involved)");
			Assert.AreEqual (false, a.Item2, "SendHandledException file doesn't exists (network involved)");
			Assert.AreEqual (true, res2, "SendHandledException success (network involved)");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestSendPing ()
		{
			Tuple<int, bool> b = GetCachedTest ("PCC", "");
			
			string uuid = UUID.GetNew ();
			string res = CacheTestPing (uuid);
			bool res2 = SendTest (res, false);
			Helpers.SleepFor (3000);

			Tuple<int, bool> a = GetCachedTest ("PCC", res);
			
			Assert.AreEqual (b.Item1, a.Item1, "SendPing file created and deleted (network involved)");
			Assert.AreEqual (false, a.Item2, "SendPing file doesn't exists (network involved)");
			Assert.AreEqual (true, res2, "SendPing success (network involved)");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
        public void TestSendEvent ()
		{
			Tuple<int, bool> b = GetCachedTest ("ECC", "");

			string uuid = UUID.GetNew ();
			string res = CacheTestEvent (uuid);
			bool res2 = SendTest (res, false);
			Helpers.SleepFor (3000);

			Tuple<int, bool> a = GetCachedTest ("ECC", res);

			Assert.AreEqual (b.Item1, a.Item1, "SendEvent file created and deleted (network involved)");
			Assert.AreEqual (false, a.Item2, "SendEvent file doesn't exists (network involved)");
			Assert.AreEqual (true, res2, "SendEvent success (network involved)");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestProcessAll ()
		{
			int n1 = 0, n2 = 0, n3 = 0, n4 = 0;
			int res = 0;

			Tuple<int, bool> t1 = GetCachedTest ("CCC", "");
			n1 = t1.Item1;
			Tuple<int, bool> t2 = GetCachedTest ("LCC", "");
			n2 = t2.Item1;
			Tuple<int, bool> t3 = GetCachedTest ("PCC", "");
			n3 = t3.Item1;
			Tuple<int, bool> t4 = GetCachedTest ("ECC", "");
			n4 = t4.Item1;
            
			string uuid = UUID.GetNew ();
			for (int i = n1; i < 20; i++)
				if (!String.IsNullOrEmpty (CacheTestCrash (uuid)))
					n1++;
			for (int i = n2; i < 20; i++)
				if (!String.IsNullOrEmpty (CacheTestHandledException (uuid)))
					n2++;
			for (int i = n3; i < 20; i++)
				if (!String.IsNullOrEmpty (CacheTestPing (uuid)))
					n3++;
			for (int i = n4; i < 20; i++)
				if (!String.IsNullOrEmpty (CacheTestEvent (uuid)))
					n4++;

			ProcessRequests proc = new ProcessRequests (uuid);
			Task.Run (async () => {
				res = await proc.Execute ();
			}).Wait ();
			Helpers.SleepFor (4000);

			Assert.AreEqual (27, res, "ProcessAll requests sent");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestUUID ()
		{
			string res = "", res2 = "";
			bool x = true;

			Task.Run (async () => {
				res = await UUIDFactory.Get ();
			}).Wait ();
			for (int i = 0; i < 20; i++) {
				Task.Run (async () => {
					res2 = await UUIDFactory.Get ();
				}).Wait ();
				x = x && (!String.IsNullOrEmpty (res2) && !String.IsNullOrWhiteSpace (res2) && res2.Equals (res));
			}

			Assert.AreEqual (true, x, "UUID persistent generator");
		}
	}
}
