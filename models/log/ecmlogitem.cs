using System;
using System.Collections.Generic;


namespace Bakera.Eccm{
	public class EcmLogItem{
		
		private DateTime myTime = default(DateTime);
		private string myData = null;
		private EcmErrorLevel myLogKind = EcmErrorLevel.Unknown;

		public EcmLogItem(string s, EcmErrorLevel logKind){
			myTime = DateTime.Now;
			myData = s;
			myLogKind = logKind;
		}

		public string Data{
			get{return myData;}
			set{myData = value;}
		}

		public DateTime Time{
			get{return myTime;}
			set{myTime = value;}
		}

		public EcmErrorLevel Kind{
			get{return myLogKind;}
			set{myLogKind = value;}
		}

		public override string ToString(){
			return string.Format("[{0}] {1} : {2}", this.Kind, this.Time, this.Data);
		}

		
	}


}

