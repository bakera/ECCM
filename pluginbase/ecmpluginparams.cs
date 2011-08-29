using System;
using System.Web;
using System.Text;

namespace Bakera.Eccm{
	public class EcmPluginParams{
		protected readonly Parser myParser;
		protected readonly Setting mySetting;
		protected readonly EcmProject myProject;
		protected readonly MarkedData myMarkedData;
		protected readonly int myCalledCount;

		public EcmPluginParams(Parser p, Setting s, EcmProject e, MarkedData incoming, int calledCount){
			myParser = p;
			mySetting = s;
			myProject = e;
			myMarkedData = incoming;
			myCalledCount = calledCount;
		}

		public Parser Parser{
			get{return myParser;}
		}

		public Setting Setting{
			get{return mySetting;}
		}

		public EcmProject Project{
			get{return myProject;}
		}

		public MarkedData MarkedData{
			get{return myMarkedData;}
		}

		public int CalledCount{
			get{return myCalledCount;}
		}

	}
}

