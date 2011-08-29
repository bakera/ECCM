using System;
using System.Collections;
using System.Collections.Specialized;

namespace Bakera.Eccm{
	public class ExportObject{

		private StringCollection myValues = new StringCollection();
		private int useCount = 0;

		public ExportObject(string val){
			myValues.Add(val);
		}

		public int Count{
			get{return myValues.Count;}
		}


		public void Add(string val){
			myValues.Add(val);
		}


		public string Get(){
			return Get(useCount++);
		}

		public string Get(int index){
			if(myValues.Count <= index) return null;
			return myValues[index];
		}

	}
}




