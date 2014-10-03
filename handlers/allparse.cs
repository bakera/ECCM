using System;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Xml;



namespace Bakera.Eccm{


	// 特定プロジェクトの全データをpublishするインターフェイスを提供します。
	public class AllParse : EcmProjectHandler{

		private static bool myParseFlag;
		private static int myCounter = 0; // publish件数カウンタ
		private static int myCounterMax = 0;

		public const string ParseAbortCommandName = "abort";

		public new const string PathName = "allparse";
		public new const string Name = "一括publish";

/*

前回一括publish時刻
前回の「このプロジェクトの」一括publishログを保存

*/

// コンストラクタ

		public AllParse(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}


// プロパティ

		public override string SubTitle{
			get{return Name;}
		}

		// 一括publishの状態や進捗を確認します。
		public override EcmResponse Get(HttpRequest rq){
			XmlNode result = myXhtml.CreateDocumentFragment();

			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "一括publishの状態";
			result.AppendChild(h3);

			if(myParseFlag){
				XmlElement p = myXhtml.Create("p", null, "現在一括publish処理中です。しばらく待ってリロードしてください。");
				result.AppendChild(p);
				XmlElement p2 = myXhtml.Create("p", null, string.Format("進捗: {0}件 / 全{1}件", myCounter, myCounterMax));
				result.AppendChild(p2);
				result.AppendChild(AbortForm());
/*
				string tempLogName = GetTempLogName();
				if(File.Exists(tempLogName)){
					XmlElement h32 = myXhtml.H(3);
					h32.InnerText = "現在までのログ" ;
					result.AppendChild(h32);
					result.AppendChild(GetLogMessage(tempLogName));
				}
*/

				return new HtmlResponse(myXhtml, result);
			}

			string resultLogName = GetResultLogName();
			if(File.Exists(resultLogName)){
				XmlElement descP = myXhtml.Create("p", null, string.Format("前回publish完了時刻は {0} です。", File.GetLastWriteTime(resultLogName)));
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
				
				XmlElement h32 = myXhtml.H(3);
				h32.InnerText = "前回publish時のログ" ;
				result.AppendChild(h32);
				result.AppendChild(GetLogMessage(resultLogName));
			} else {
				XmlElement descP = myXhtml.Create("p", null, "一括publishは行われていません。");
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
			}

			return new HtmlResponse(myXhtml, result);
		}

		private XmlElement GetParseButton(){
			XmlElement form = myXhtml.Form(null, "post");

			XmlElement descP = myXhtml.P();
			descP.InnerText = "このプロジェクトの一括publishを開始するには、「一括publish開始」ボタンを押してください。" ;
			form.AppendChild(descP);

			XmlElement formP = myXhtml.P();
			XmlElement parseSubmit = myXhtml.CreateSubmit("一括publish開始");
			formP.AppendChild(parseSubmit);
			form.AppendChild(formP);
			return form;
		}

		private XmlNode GetLogMessage(string filename){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string logMessage = "";
			string infoStr = string.Format("[{0}]", EcmErrorLevel.Information);

			using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, Util.SjisEncoding)){
					while (sr.Peek() >= 0) {
						string temp = sr.ReadLine();
						if(string.IsNullOrEmpty(temp)) continue;
						if(temp.StartsWith(infoStr)) continue;
						logMessage += temp + "\n";
					}
					sr.Close();
				}
				fs.Close();
			}

			XmlElement pre = myXhtml.Create("pre");
			pre.InnerText = logMessage;
			result.AppendChild(pre);

			return result;
		}



		// Postを処理して一括publishを実行または中止します。
		public override EcmResponse Post(HttpRequest rq){
			if(rq.Form[ParseAbortCommandName] != null){
				return AbortAllParse(rq);
			}
			return StartAllParse(rq);
		}


// プライベートメソッド

		// 一括publishを実行します。
		private EcmResponse StartAllParse(HttpRequest rq){

			XmlNode result = myXhtml.CreateDocumentFragment();

			if(myParseFlag){
				XmlElement h3 = myXhtml.H(3);
				h3.InnerText = "publishできません";
				result.AppendChild(h3);

				XmlElement p = myXhtml.P();
				p.InnerText = "現在一括publish処理中のため、一括publishを実行することはできません。";
				p.InnerText += string.Format("進捗: {0}件 / 全{1}件", myCounter, myCounterMax);
				result.AppendChild(p);
			} else {
				myParseFlag = true;
				Thread t = new Thread(new ThreadStart(ExecuteAllParse));
				t.Start();

				// 少しだけ待ってみる

				for(int i=0; i < 5; i++){
					if(!t.IsAlive) return new HtmlResponse(myXhtml, EndMessage());
					Thread.Sleep(500);
				}

				// 終わらなかった
				XmlElement h3 = myXhtml.H(3);
				h3.InnerText = "一括publish開始";
				result.AppendChild(h3);

				XmlElement p = myXhtml.P();
				p.InnerText = "一括publish処理を開始しましたが、処理に時間がかかっています。処理はバックグラウンドで進行中です。進捗や結果の確認をする場合は、";

				XmlElement a = myXhtml.Create("a");
				a.SetAttribute("href", PathName);
				a.InnerText = "一括publishの状態";
				p.AppendChild(a);

				XmlText tx = myXhtml.Text("画面に戻り、リロードしてみてください。");
				p.AppendChild(tx);

				result.AppendChild(p);

				result.AppendChild(AbortForm());
			}

			return new HtmlResponse(myXhtml, result);
		}


		private EcmResponse AbortAllParse(HttpRequest rq){
			FileInfo abortCommandFile = new FileInfo(GetAbortCommandFileName());

			if(abortCommandFile.Exists){
				abortCommandFile.LastWriteTime = DateTime.Now;
			} else {
				 using (FileStream fs = abortCommandFile.Create()){}
			}

			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement p = myXhtml.P();
			p.InnerText = "一括publish処理を中止しました。";

			XmlElement a = myXhtml.Create("a");
			a.SetAttribute("href", PathName);
			a.InnerText = "一括publishの状態";
			p.AppendChild(a);

			XmlText tx = myXhtml.Text("画面に戻り、リロードしてみてください。");
			p.AppendChild(tx);
			result.AppendChild(p);

			return new HtmlResponse(myXhtml, result);
		}


		private XmlNode EndMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "一括publish完了";
			result.AppendChild(h3);

			XmlElement p = myXhtml.P();
			p.InnerText = "一括publish処理が完了しました。";
			result.AppendChild(p);

			XmlElement h32 = myXhtml.H(3);
			h32.InnerText = "publish時のログ" ;
			result.AppendChild(h32);

			result.AppendChild(GetLogMessage(GetResultLogName()));
			
			return result;
		}

		private XmlNode AbortForm(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement form = myXhtml.Form(null, "post");
			XmlElement descP = myXhtml.P();
			descP.InnerText = "このプロジェクトの一括publishを中止するには、「一括publish中止」ボタンを押してください。" ;
			form.AppendChild(descP);
			XmlElement formP = myXhtml.P();
			XmlElement abortSubmit = myXhtml.CreateSubmit("一括publish中止");
			abortSubmit.SetAttribute("name", ParseAbortCommandName);
			formP.AppendChild(abortSubmit);
			form.AppendChild(formP);
			result.AppendChild(form);
			return result;
		}


		// 一括publishを実行します。
		private void ExecuteAllParse(){
			myParseFlag = true;
			string logFile = GetTempLogName();
			string resultFile = GetResultLogName();

			FileInfo abortCommandFile = new FileInfo(GetAbortCommandFileName());
			DateTime startTime = DateTime.Now;

			try{
				EcmItem[] items = myProject.GetAllItems();
				myCounter = 0;
				myCounterMax = items.Length;

				foreach(EcmItem i in  items){
					if(i.ParsePermit == false){
						string mes = string.Format("{0} : {1} をスキップします (publish許可条件{2}を満たしません)。\n\n", DateTime.Now, i, myProject.Setting.ParsePermissonRule);
						Util.AppendWriteFile(logFile, mes);
						continue;
					}

					// パーサを用意
					Parser p = new Parser(myProject);
					// information は記録しない
					p.Log.MinimumErrorLevel = EcmErrorLevel.Important;

					ProcessResult pr = p.Process(i);
					Interlocked.Increment(ref myCounter);

					string parseStartMes = string.Format("{0} : {1} publish開始\n", DateTime.Now, i);
					Util.AppendWriteFile(logFile, parseStartMes);
					Util.AppendWriteFile(logFile, p.Log.ToString());
					if(p.Log.ErrorLevel < EcmErrorLevel.Error){
						string parseEndMes = string.Format("{0} : {1} publish完了\n", DateTime.Now, i);
						Util.AppendWriteFile(logFile, parseEndMes);
					}
					Util.AppendWriteFile(logFile, string.Format("{0} : {1}\n", i, pr.Message));

					// 中断コマンドが発令されていたら中止する
					abortCommandFile.Refresh();
					if(abortCommandFile.Exists){
//						Util.AppendWriteFile(logFile, string.Format("{0} / {1}", abortCommandFile.LastWriteTime , startTime));
						if(abortCommandFile.LastWriteTime > startTime){
							Util.AppendWriteFile(logFile, myProject.Id + " : AllParse aborted");
							return;
						}
					}
				}
				Util.AppendWriteFile(logFile, myProject.Id + " : AllParse end");
			} catch(Exception e) {
				Util.AppendWriteFile(logFile, e.ToString());
			} finally {
				File.Copy(logFile, resultFile, true);
				File.Delete(logFile);
				myParseFlag = false;
			}
		}


		// 一括publish中に進捗を書き出すためのテンポラリログファイル名を取得します。
		private string GetTempLogName(){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("プロジェクトディレクトリが存在しません : " + logDir);
			return logDir.TrimEnd('\\') + "\\allparse.temp";
		}

		// 一括publish中に中断コマンドを受け取るためのテンポラリログファイル名を取得します。
		// このファイルは全プロジェクトで共通です。
		private string GetAbortCommandFileName(){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("プロジェクトディレクトリが存在しません : " + logDir);
			return logDir.TrimEnd('\\') + "\\allparseabort.txt";
		}

		// 最終的に書き出すログファイル名を取得します。
		// このディレクトリは各プロジェクトごとに異なります。
		private string GetResultLogName(){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("プロジェクトディレクトリが存在しません : " + logDir);
			return logDir.TrimEnd('\\') + "\\allparse.log";
		}




	}
}


