using BugSense.Extensions;

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
	internal class TestInternal2
	{
#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesExistFalse ()
		{
			bool x = true;

			Task.Run (async () => {
				await Files.Delete ("xxx", "zzz");
			}).Wait ();

			Task.Run (async () => {
				x = await Files.Exists (Path.Combine ("xxx", "zzz"));
			}).Wait ();

			Assert.AreEqual (false, x, "Non-existent file");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesExistTrue ()
		{
			bool x = false, y = false;
			
			Task.Run (async () => {
				x = await Files.CreatWriteTo ("xxx", "yyy", "Hello world!\n");
			}).Wait ();

			Task.Run (async () => {
				y = await Files.Exists (Path.Combine ("xxx", "yyy"));
			}).Wait ();
			
			Assert.AreEqual (true, x && y, "Existent file");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesFromDirectory ()
		{
			List<string> x = null;
			
			Task.Run (async () => {
				x = await Files.GetDirFilenames ("xxx", "yy");
				foreach (string f in x)
					await Files.Delete ("xxx", f);
			}).Wait ();
			
			Task.Run (async () => {
				await Files.CreatWriteTo ("xxx", "yyy", "Hello world!\n");
				await Files.CreatWriteTo ("xxx", "yyy2", "Hello world!\n");
				await Files.CreatWriteTo ("xxx", "yyy3", "Hello world!\n");
				await Files.CreatWriteTo ("xxx", "4yyy", "Hello world!\n");
			}).Wait ();
			
			Task.Run (async () => {
				x = await Files.GetDirFilenames ("xxx", "yy");
			}).Wait ();
			
			Assert.AreEqual (3, x.Count, "Get directory files");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesCreatOrWr ()
		{
			bool x = false;

			Task.Run (async () => {
				x = await Files.CreatWriteTo ("xxx", "yyy", "Hello world!\n");
				x = x && await Files.CreatWriteTo ("xxx", "yyy2", "Hello world!\n");
				x = x && await Files.CreatWriteTo ("xxx", "yyy3", "Hello world!\n");
				x = x && await Files.CreatWriteTo ("xxx", "4yyy", "Hello world!\n");
			}).Wait ();

			Assert.AreEqual (true, x, "Create directory with file(s)");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesReadOk ()
		{
			Tuple<string, bool> x = null;

			Task.Run (async () => {
				await Files.CreatWriteTo ("xxx", "yyy", "Hello world!\n");
			}).Wait ();

			Task.Run (async () => {
				x = await Files.ReadFrom (Path.Combine ("xxx", "yyy"));
			}).Wait ();

			Assert.AreEqual (true, x.Item2, "Read non-existent file result");
			Assert.AreEqual ("Hello world!\n", x.Item1, "Read non-existent file text");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesReadNotOk ()
		{
			Tuple<string, bool> x = null;
			
			Task.Run (async () => {
				await Files.Delete ("xxx", "zzz");
			}).Wait ();

			Task.Run (async () => {
				x = await Files.ReadFrom (Path.Combine ("xxx", "zzz"));
			}).Wait ();
			
			Assert.AreEqual (false, x.Item2, "Read non-existent file result");
			Assert.AreEqual ("", x.Item1, "Read non-existent file text");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestFilesDeleteOk ()
		{
			bool x1 = false, x2 = true, x3 = true;

			Task.Run (async () => {
				await Files.CreatWriteTo ("xxx", "xy", "test");
				x1 = await Files.Exists (Path.Combine ("xxx", "xy"));
				await Files.Delete ("xxx", "xy");
				x2 = await Files.Exists (Path.Combine ("xxx", "xy"));
			}).Wait ();
			Task.Run (async () => {
				x3 = await Files.Exists (Path.Combine ("xxx", "xy"));
			}).Wait ();

			Assert.AreEqual (true, x1, "Existent file");
			Assert.AreEqual (false, x2, "Deleted file");
			Assert.AreEqual (false, x3, "Deleted file");
		}
	}
}
