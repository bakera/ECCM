using System;
using System.Threading;

namespace Bakera.Eccm{
	public class TestProcessor : EcmProcessorBase{

		public new const string Name = "テストプロセッサ";
		public new const string Description = "ECCMプロセッサのテスト用です。単に5秒待ち、何もしません。";

// コンストラクタ
		public TestProcessor(EcmProject proj) : base(proj){}

// public メソッド

		// 与えられた EcmItem に対応するファイルを Parse して置換します。
		public override ProcessResult Process(EcmItem targetItem){
			Log.AddInfo("ID: {0} 処理開始", targetItem.Id);
			Thread.Sleep(5000);
			Log.AddInfo("ID: {0} 処理終了", targetItem.Id);

			ProcessResult result = new ProcessResult();
			return result;
		}

	}


}




