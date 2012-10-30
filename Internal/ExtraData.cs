using System;
using System.Collections.Generic;

namespace BugSense.Internal
{
	public class ExtraData
	{
		public ExtraData ()
		{
		}

		private Dictionary<string, string> sdict = new Dictionary<string, string> ();
		private const int MAXLEN = 128;
		private int count = 0;
		private const int MAXCOUNT = 32;

        public Dictionary<string, string> Get()
        {
            return this.sdict;
        }

		public void Set (Dictionary<string, string> sdict=null)
		{
			if (sdict == null)
				this.sdict = new Dictionary<string, string> ();
			else {
				if (sdict.Count <= MAXCOUNT)
					this.sdict = new Dictionary<string, string> (sdict);
				else {
					this.sdict = new Dictionary<string, string> ();
					int i = 0;
					foreach (var pair in sdict) {
						this.sdict [pair.Key] = pair.Value;
						i++;
						if (i >= MAXCOUNT)
							break;
					}
				}
			}
			
			this.count = this.sdict.Count;
		}
		
		public bool AddTo (string akey, string avalue)
		{
			string key, value;
			
			if (akey == null || avalue == null)
				return false;
			
			key = akey.Trim ();
			key = key.Substring (0, key.Length <= MAXLEN ? key.Length : MAXLEN);
			value = avalue.Trim ();
			value = value.Substring (0, value.Length <= MAXLEN ? value.Length : MAXLEN);
			
			if (!this.sdict.ContainsKey (key) && this.count >= MAXCOUNT)
				return false;
			else if (!this.sdict.ContainsKey (key))
				this.count++;
			
			try {
				this.sdict.Add (key, value);
			} catch (ArgumentException) {
				this.sdict [key] = value;
			} catch (Exception) {
				return false;
			}

			return true;
		}
		
		public bool RemoveFrom (string akey)
		{
			bool res = false;
            string key;
			
			if (akey == null || this.count == 0)
				return false;

            key = akey.Trim();
            key = key.Substring(0, key.Length <= MAXLEN ? key.Length : MAXLEN);
            
            res = (bool)(sdict.Remove(key));
			
			if (res == true)
				this.count--;
			
			return res;
		}
		
		public void Print ()
		{
			foreach (var pair in this.sdict)
				Console.WriteLine ("{0}, {1}", pair.Key, pair.Value);
			Console.WriteLine ("count = {0}", this.count);
		}
	}
}
