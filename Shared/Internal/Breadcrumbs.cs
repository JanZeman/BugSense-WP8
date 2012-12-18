using BugSense.Extensions;
using System;

namespace BugSense.Internal
{
	internal class Breadcrumbs
	{
		#region [ Attributes ]
		public string[] Arr { get; private set; }

		private const int MAXLEN = 64;

		public int Count { get; private set; }

		private const int MAXCOUNT = 16;
		#endregion

		#region [ Ctor ]
		public Breadcrumbs ()
		{
			Reset ();
		}
		#endregion

		#region [ Public Methods ]
		public void Reset ()
		{
			Count = 0;
			Arr = new string[MAXCOUNT];
		}

		public bool AppendTo (string val)
		{
			string value;

			if (String.IsNullOrEmpty (val))
				return false;

			value = NormalizeElement (val);
			if (value [0] == '_') {
				char[] tmp = value.ToCharArray ();
				tmp [0] = '-';
				value = new string (tmp);
			}
			value = value.Replace ("|", "-");

			if (Count >= MAXCOUNT) {
				for (int i = 1; i < MAXCOUNT; i++)
					Arr [i - 1] = Arr [i];
				Arr [MAXCOUNT - 1] = value;
			} else {
				Arr [Count] = value;
				Count++;
			}

			return true;
		}

		override public string ToString ()
		{
			string res = new string ("".ToCharArray ());

			if (Count <= 0)
				return res;

			res += Arr [0];
			for (int i = 1; i < Count; i++)
				res += "|" + Arr [i];

			return res;
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
		#endregion
	}
}
