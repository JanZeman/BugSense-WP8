using BugSense.Extensions;
using System;
using System.Collections.Generic;

namespace BugSense.Internal
{
	internal class ExtraData
	{
		#region [ Attributes ]
		public Dictionary<string, string> Dict { get; private set; }

		private const int MAXLEN = 128;

		public int Count { get; private set; }

		private const int MAXCOUNT = 32;
		#endregion

		#region [ Ctor ]
		public ExtraData ()
		{
			Set ();
		}

		public ExtraData (Dictionary<string, string> dict)
		{
			Set (dict);
		}
		#endregion

		#region [ Public Methods ]
		public void Set (Dictionary<string, string> sdict=null)
		{
			Dict = new Dictionary<string, string> ();
			Count = 0;

			if (sdict != null)
				foreach (var pair in sdict) {
					if (String.IsNullOrEmpty (pair.Key) || String.IsNullOrEmpty (pair.Value))
						continue;
					var newpair = NormalizePair (pair);
					Dict [newpair.Key] = newpair.Value;
					Count++;
					if (Count >= MAXCOUNT)
						break;
				}
		}
		
		public bool AddTo (string akey, string avalue)
		{
			string key, value;
			
			if (String.IsNullOrEmpty (akey) || String.IsNullOrEmpty (avalue))
				return false;
			
			key = NormalizeElement (akey);
			value = NormalizeElement (avalue);

			if (!Dict.ContainsKey (key) && Count >= MAXCOUNT)
				return false;
			else if (!Dict.ContainsKey (key))
				Count++;
			
			try {
				Dict.Add (key, value);
			} catch (ArgumentException) {
				Dict [key] = value;
			} catch (Exception) {
				return false;
			}

			return true;
		}

		public bool AddTo (Dictionary<string, string> dict)
		{
			if (dict == null)
				return false;

			foreach (var pair in dict)
				AddTo (pair.Key, pair.Value);

			return true;
		}
		
		public bool RemoveFrom (string akey)
		{
			bool res = false;
			string key;
			
			if (String.IsNullOrEmpty (akey) || Count == 0)
				return false;

			key = NormalizeElement (akey);            

			res = (bool)(Dict.Remove (key));
			if (res == true)
				Count--;
			
			return res;
		}

		override public string ToString ()
		{
			string str = "{";

			foreach (var pair in Dict)
				str += "{\"" + pair.Key + "\", \"" + pair.Value + "\"}";
			str += "}";

			return str;
		}

		public void Print ()
		{
			Helpers.Log (ToString () + " [count = " + Count + "]");
		}
		#endregion

		#region [ Private Methods ]
		private string NormalizeElement (string elm)
		{
			string tmp;

			tmp = elm.Trim ();
			tmp = tmp.Substring (0, tmp.Length <= MAXLEN ? tmp.Length : MAXLEN);

			return tmp;
		}

		private KeyValuePair<string, string> NormalizePair (KeyValuePair<string, string> kv)
		{
			return new KeyValuePair<string, string> (NormalizeElement (kv.Key), NormalizeElement (kv.Value));
		}
		#endregion
	}
}
