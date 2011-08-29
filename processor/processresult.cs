using System;
using System.Collections.Generic;

namespace Bakera.Eccm{


	// ˆ—Œ‹‰Ê‚ğŠi”[‚·‚éƒNƒ‰ƒX
	public class ProcessResult{

		private List<string> myError = new List<string>();
		private string myResult;
		private string myMessage;

		public string[] Errors{
			get{return myError.ToArray();}
		}

		public string Result{
			get{return myResult;}
			set{myResult = value;}
		}

		public string Message{
			get{return myMessage;}
			set{myMessage = value;}
		}

		public void AddError(string format, params string[] datas){
			string s = string.Format(format, datas);
			myError.Add(s);
		}


	}
}




