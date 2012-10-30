/*
 * UDID entropy-based generator
 * by dgk@bugsense.com
 * 2012-05-11
 *
 * C# sample implementation
 */

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace EntropyUUID
{
    class UUID
    {
        private static RNGCryptoServiceProvider _global = new RNGCryptoServiceProvider();
        
        public static string getNew()
        {
            // STEP 1: get time since Epoch.
            // OUTPUT: timestamp in millisecond (or finer) granularity.
            // RATIONALE: collision only when app is deployed at the exact same
            //   time.
            String s1 = DateTime.Now.Millisecond.ToString();

            // STEP 2: get an object reference.
            // OUTPUT: memory address or another distinguishable object property.
            // RATIONALE: very little probability for two runs to get the
            //   same address (debatable when in a sandbox).
            object o = new object();
            String s2 = o.GetHashCode().ToString();

            // STEP 3: sleep for a little time.
            // OUTPUT: time delta in _finer_ millisecond granularity.
            // RATIONALE: os fine timing granularity is not guaranteed
            //   (debatable when dealing with real-time/hard real-time
            //   kernels).
            DateTime dt = DateTime.Now;
            Thread.Sleep(256);
            DateTime dt2 = DateTime.Now;
            String s3 = (dt2.Ticks - dt.Ticks).ToString();

            // STEP 4: generate a random number.
            // OUTPUT: random integer.
            // RATIONALE: the pseudo-random generator may be the same, but there
            //   is a slight chance that something might have changed in the
            //   seed (may be a second passed in some cases).
            //   Also, some systems (e.g. a JVM) have their own pseudo-random
            //   generators that are "fed" with their own implementations,
            //   narrowing the probability for same results.
            byte[] buffer = new byte[4];
            _global.GetBytes(buffer);
            string s4 = ((new Random(BitConverter.ToInt32(buffer, 0))).Next() % 65536).ToString();

            // STEP 5: do a complementary step.
            // OUTPUT: an integer in [0,9].
            // RATIONALE: when all steps are done, we get a modulo of the current time.
            //   It's high unlikely that every time the steps should have finished
            //   precisely at the same time unit. Thus, the modulo will likely vary.
            long tt5 = DateTime.Now.Ticks % 10;
            string s5 = tt5.ToString();

            // The output string.
            string sall = s1 + s2 + s3 + s4 + s5;

            // SHA1 from output string.
            return GetSHA1Hash(sall);
        }

        private static string GetSHA1Hash(string input)
        {
            SHA1Managed s = new SHA1Managed();
            UTF8Encoding enc = new UTF8Encoding();
            s.ComputeHash(enc.GetBytes(input.ToCharArray()));

            return BitConverter.ToString(s.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
