using BugSense.Internal;

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

namespace BugSense.UnitTests
{
#if __MonoCS__
	[TestFixture()]
	#else
	[TestClass]
#endif
	internal class TestInternal1
	{
#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestExtraData ()
		{
			ExtraData data = new ExtraData (new Dictionary<string, string> () {
				{"k1", "v1"},
				{"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
					"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
						"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
					"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
						"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
							"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"},
				{"", ""}
			});
			Assert.AreEqual (2, data.Count, "With long input");

			data.Set ();
			Assert.AreEqual (0, data.Count, "Reset to empty");
			Assert.AreEqual ("{}", data.ToString (), "Reset to empty");

			data.AddTo ("k1", "v1");
			data.AddTo (new Dictionary<string, string> () {{"k2", "v2"}, {"k1", "v11"}, {"", "xxx"}});
			Assert.AreEqual ("{{\"k1\", \"v11\"}{\"k2\", \"v2\"}}", data.ToString (), "ToString()");

			for (int i=100; i<1000; i++)
				data.AddTo ("kk" + i.ToString (), "vv" + i.ToString ());
			Assert.AreEqual (32, data.Count, "Max size");
		}

#if __MonoCS__
		[Test()]
		#else
		[TestMethod]
#endif
		public void TestBreadcrumbs ()
		{
			Breadcrumbs crumbs = new Breadcrumbs ();
			Assert.AreEqual (0, crumbs.Count, "Empty");
			Assert.AreEqual ("", crumbs.ToString (), "Empty ToString()");

			crumbs.AppendTo ("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
			Assert.AreEqual ("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", crumbs.ToString (),
			                "Long input");

			crumbs.Reset ();
			crumbs.AppendTo ("_e1");
			crumbs.AppendTo ("e_2");
			crumbs.AppendTo ("e|3");
			crumbs.AppendTo ("||e4||");
			crumbs.AppendTo ("___e|5___");
			Assert.AreEqual ("-e1|e_2|e-3|--e4--|-__e-5___", crumbs.ToString (), "Weird input");
			Assert.AreEqual (5, crumbs.Count, "Weird count");

			crumbs.AppendTo ("");
			Assert.AreEqual (5, crumbs.Count, "Append empty");
		}
	}
}
