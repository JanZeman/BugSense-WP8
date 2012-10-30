using System;

namespace BugSense.Internal
{
	public class Breadcrumbs
	{
		public Breadcrumbs ()
		{
		}

		private string[] sarray = new string[MAXCOUNT];
		private const int MAXLEN = 64;
		private int count = 0;
		private const int MAXCOUNT = 16;

		public void Reset ()
		{
			this.count = 0;
		}

		public bool AppendTo (string val)
		{
			string value;

			if (val == null)
				return false;

			value = val.Trim ();
			value = value.Substring (0, value.Length <= MAXLEN ? value.Length : MAXLEN);
			if (value [0] == '_') {
				char[] tmp = value.ToCharArray ();
				tmp [0] = '-';
				value = new string (tmp);
			}
			value.Replace ("|", "_");

			if (this.count >= MAXCOUNT) {
				for (int i=1; i<MAXCOUNT; i++)
					this.sarray [i - 1] = this.sarray [i];
				this.sarray [MAXCOUNT - 1] = value;
			} else {
				this.sarray [this.count] = value;
				this.count++;
			}

			return true;
		}

        public string Reduce()
        {
            string res = new string("".ToCharArray());

            if (this.count <= 0)
                return res;

            res += this.sarray[0];
            for (int i = 1; i < this.count; i++)
                res += "|" + this.sarray[i];

            return res;
        }

		public void Print ()
		{
			for(int i=0; i<this.count; i++)
				Console.WriteLine ("{0}", this.sarray[i]);
			Console.WriteLine ("count = {0}", this.count);
		}
	}
}
