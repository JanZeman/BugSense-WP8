/*
 * UDID entropy-based generator
 * by dgk@bugsense.com
 * 2012-05-11
 *
 * C# sample implementation
 */

using BugSense.Extensions;
using System;
#if WINDOWS_PHONE
using System.Security.Cryptography;
using System.Text;
#elif NETFX_CORE
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
#else
using System.Security.Cryptography;
using System.Text;
#endif

namespace EntropyUUID
{
    internal class UUID
    {
#if WINDOWS_PHONE
        private static RNGCryptoServiceProvider _global = new RNGCryptoServiceProvider();
#endif

        public static string GetNew()
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
            Helpers.SleepFor(256);
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
#if WINDOWS_PHONE
            byte[] buffer = new byte[4];
            _global.GetBytes(buffer);
            string s4 = ((new Random(BitConverter.ToInt32(buffer, 0))).Next() % 65536).ToString();
#else
            string s4 = (new Random().Next() % 65536).ToString();
#endif

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
#if WINDOWS_PHONE
            SHA1Managed s = new SHA1Managed();
            UTF8Encoding enc = new UTF8Encoding();
            s.ComputeHash(enc.GetBytes(input.ToCharArray()));

            return BitConverter.ToString(s.Hash).Replace("-", "").ToLowerInvariant();
#elif NETFX_CORE
            // Convert the message string to binary data.
            IBuffer buffUtf8Msg = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);

            // Create a HashAlgorithmProvider object.
            HashAlgorithmProvider objAlgProv = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

            // Demonstrate how to retrieve the name of the hashing algorithm.
            String strAlgNameUsed = objAlgProv.AlgorithmName;

            // Hash the message.
            IBuffer buffHash = objAlgProv.HashData(buffUtf8Msg);

            // Verify that the hash length equals the length specified for the algorithm.
            //if (buffHash.Length != objAlgProv.HashLength)
            //{
            //    throw new Exception("There was an error creating the hash");
            //}

            // Convert the hash to a string (for display).
            //String strHashBase64 = CryptographicBuffer.EncodeToBase64String(buffHash);
            string str = CryptographicBuffer.EncodeToHexString(buffHash);

            // Return the encoded string
            return str.ToLowerInvariant();
#else
			SHA1Managed s = new SHA1Managed();
			UTF8Encoding enc = new UTF8Encoding();
			s.ComputeHash(enc.GetBytes(input.ToCharArray()));
			
			return BitConverter.ToString(s.Hash).Replace("-", "").ToLowerInvariant();
#endif
        }
    }
}
