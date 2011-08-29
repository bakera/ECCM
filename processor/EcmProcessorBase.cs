using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Bakera.Eccm{
	public abstract class EcmProcessorBase{

		private Setting mySetting;
		private EcmProject myProject;
		private EcmLog myLog = new EcmLog();

		public const string Name = null;
		public const string Description = null;


// コンストラクタ
		public EcmProcessorBase(EcmProject proj){
			myProject = proj;
			mySetting = proj.Setting;
		}


// プロパティ

		public EcmProject Project{
			get{return myProject;}
		}

		public Setting Setting{
			get{return mySetting;}
		}

		public EcmLog Log{
			get{return myLog;}
		}


// public メソッド
		// 与えられた EcmItem に対応するファイルを Parse して置換します。
		public abstract ProcessResult Process(EcmItem targetItem);


	}
}




