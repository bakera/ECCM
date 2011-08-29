using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Serialization;


namespace Bakera.Eccm{


	// プロジェクト全体を処理する一括処理ツールを扱います。
	public class AllProcess : EcmList{

		public AllProcess(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "一括処理";
		public new const string PathName = "allprocess";

		private static Type myProcessorType;
		private static Thread myThread;
		private EventWaitHandle myProcessorHandler;
		private const int WaitTime = 5000;// 待ち時間

		private static int myCounter = 0; // publish件数カウンタ
		private static int myCounterMax = 0;


// プロパティ
		public override string SubTitle{
			get{return Name;}
		}

		public Type ProcessorType{
			get{return myProcessorType;}
		}


// Get/Post メソッド

		public override EcmResponse Get(HttpRequest rq){
			XmlNode result = myXhtml.CreateDocumentFragment();

			if(myThread != null && myThread.IsAlive){
				if(ProcessorType != null){
					XmlElement p = myXhtml.Create("p", null, "現在" + ProcessorType.Name + "による一括処理中です。しばらく待ってリロードしてください。");
					result.AppendChild(p);
				}
				XmlElement p2 = myXhtml.Create("p", null, string.Format("進捗: {0}件 / 全{1}件", myCounter, myCounterMax));
				result.AppendChild(p2);
				result.AppendChild(AbortForm());
				return new HtmlResponse(myXhtml, result);
			}

			string[] options = GetOptions(rq);
			Type pType = null;
			if(options.Length > 0){
				pType = GetProcessorType(options[0]);
			}

			if(pType == null){
				XmlElement ul = myXhtml.Create("ul");
				foreach(Type t in Setting.ProcessorTypes){
					XmlElement li = myXhtml.Create("li");
					string hrefStr = PathName + "/" + t.Name;
					XmlElement a = myXhtml.Create("a");
					a.SetAttribute("href", hrefStr);
					a.InnerText = Util.GetFieldValue(t, "Name") + " / " + t.Name;
					li.AppendChild(myXhtml.P(null, a));
					li.AppendChild(myXhtml.P(null, Util.GetFieldValue(t, "Description")));
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
				return new HtmlResponse(myXhtml, result);
			}

			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = pType.Name + "一括処理の状態";
			result.AppendChild(h3);

			string resultLogName = GetResultLogName(pType.Name);
			if(File.Exists(resultLogName)){
				XmlElement descP = myXhtml.Create("p", null, string.Format("処理は終了しています。前回の処理終了時刻は {0} です。", File.GetLastWriteTime(resultLogName)));
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
				result.AppendChild(PreviousLog(pType));
			} else {
				XmlElement descP = myXhtml.Create("p", null, "一括処理は行われていません。");
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
			}

			return new HtmlResponse(myXhtml, result);
		}

		private XmlElement GetParseButton(){
			XmlElement form = myXhtml.Form(null, "post");

			XmlElement descP = myXhtml.P();
			descP.InnerText = "このプロジェクトの一括処理を開始するには、「一括処理開始」ボタンを押してください。" ;
			form.AppendChild(descP);

			XmlElement formP = myXhtml.P();
			XmlElement parseSubmit = myXhtml.CreateSubmit("一括処理開始");
			formP.AppendChild(parseSubmit);
			form.AppendChild(formP);
			return form;
		}



		// 一括処理を実行/停止します。
		public override EcmResponse Post(HttpRequest rq){
			string[] options = GetOptions(rq);
			Type pType = null;
			if(options.Length > 0){
				pType = GetProcessorType(options[0]);
			}

			// 中止
			string action = rq["action"];
			if(!string.IsNullOrEmpty(action)){
				if(action.Equals("abort", StringComparison.CurrentCultureIgnoreCase)){
					if(myThread == null || !myThread.IsAlive) return Get(rq);
					myThread.Abort();
					return new HtmlResponse(myXhtml, ThreadAbortMessage());
				}
			}

			if(pType == null) return new HtmlResponse(myXhtml, NoHandlerErrorMessage());
			if(myThread != null && myThread.IsAlive) return new HtmlResponse(myXhtml, ProcessingErrorMessage());

			// 開始
			myProcessorHandler = new EventWaitHandle(false, EventResetMode.AutoReset);
			myProcessorType = pType;
			myThread = new Thread(new ThreadStart(ExecuteAllProcess));
			myThread.Start();
			
			// ちょっと待つ
			bool waitResult = WaitHandle.WaitAll(new WaitHandle[]{myProcessorHandler}, WaitTime, false);
			if(waitResult) return new HtmlResponse(myXhtml, EndMessage());

			// 終わらなかった
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "一括処理開始";
			result.AppendChild(h3);

			XmlElement p = myXhtml.P();
			p.InnerText = pType.Name + "による一括処理を開始しましたが、処理に時間がかかっています。処理はバックグラウンドで進行中です。進捗や結果の確認をする場合は、";

			XmlElement a = myXhtml.Create("a");
			a.SetAttribute("href", ProcessorType.Name);
			a.InnerText = ProcessorType.Name + "一括処理の状態";
			p.AppendChild(a);

			XmlText tx = myXhtml.Text("画面に戻り、リロードしてみてください。");
			p.AppendChild(tx);

			XmlElement p2 = myXhtml.Create("p", null, string.Format("進捗: {0}件 / 全{1}件", myCounter, myCounterMax));
			result.AppendChild(p2);
			result.AppendChild(p);
			result.AppendChild(AbortForm());

			return new HtmlResponse(myXhtml, result);
		}



// プライベートメソッド

		private XmlNode EndMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = ProcessorType.Name + "一括処理完了";
			result.AppendChild(h3);

			XmlElement p = myXhtml.P();
			p.InnerText = "一括処理が完了しました。";
			result.AppendChild(p);
			result.AppendChild(PreviousLog(ProcessorType));

			return result;
		}

		private XmlNode PreviousLog(Type t){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "ログ";
			result.AppendChild(h3);
			result.AppendChild(GetLogMessage(GetResultLogName(t.Name)));
			return result;
		}


		private XmlNode NoHandlerErrorMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3unkown = myXhtml.H(3);
			h3unkown.InnerText = "処理できません";
			result.AppendChild(h3unkown);
			XmlElement punknown = myXhtml.P();
			punknown.InnerText = "不明なハンドラです。処理を実行することはできません。";
			result.AppendChild(punknown);
			return result;
		}

		private XmlNode ProcessingErrorMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "処理できません";
			result.AppendChild(h3);
			XmlElement p = myXhtml.P();
			if(myProcessorType != null){
				p.InnerText = "現在" + myProcessorType.Name + "の一括処理中のため、新たな処理を実行することはできません。処理が終わるまで待ってください。";
			}
			p.InnerText += string.Format("進捗: {0}件 / 全{1}件", myCounter, myCounterMax);
			result.AppendChild(p);
			result.AppendChild(AbortForm());
			return result;
		}

		private XmlNode ThreadAbortMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "一括処理の強制終了";
			result.AppendChild(h3);
			XmlElement p = myXhtml.P();
			if(myProcessorType != null){
				p.InnerText = ProcessorType.Name + "の一括処理を強制終了しました。";
			}
			result.AppendChild(p);
			result.AppendChild(PreviousLog(ProcessorType));
			return result;
		}


		private XmlNode AbortForm(){
			XmlElement form = myXhtml.Form(null, "post");
			XmlElement abortSubmit = myXhtml.CreateSubmit("一括処理を強制終了する");
			XmlNode abortHidden = myXhtml.Hidden("action", "abort");
			XmlElement formP = myXhtml.P(null,  abortHidden, abortSubmit);
			form.AppendChild(formP);
			return form;
		}


		protected string[] GetOptions(HttpRequest rq){
			string thisPath = "/" + myProject.Id + "/" + PathName + "/";
			string optionId = Util.CutLeft(rq.PathInfo, thisPath);
			string[] optionIds = optionId.Split('/');
			return optionIds;
		}


		private XmlNode GetLogMessage(string filename){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string infoStr = string.Format("[{0}]", EcmErrorLevel.Information);
			XmlElement ol = myXhtml.Create("ol");

			using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, Util.SjisEncoding)){
					while (sr.Peek() >= 0) {
						string temp = sr.ReadLine();
						XmlElement li = myXhtml.Create("li");
						if(temp.StartsWith(infoStr)) li.SetAttribute("class", "info");
						li.InnerText = temp;
						ol.AppendChild(li);
					}
					sr.Close();
				}
				fs.Close();
			}
			result.AppendChild(ol);
			return result;
		}


		// 一括処理を実行します。
		private void ExecuteAllProcess(){
			string logFile = GetTempLogName(myProcessorType.Name);
			string resultFile = GetResultLogName(myProcessorType.Name);

			try{
				EcmItem[] items = myProject.GetAllItems();
				myCounter = 0;
				myCounterMax = items.Length;
				foreach(EcmItem i in items){
					if(i.ParsePermit == false){
						string mes = string.Format("{0} : {1} をスキップします (publish許可条件{2}を満たしません)。\n", DateTime.Now, i, myProject.Setting.ParsePermissonRule);
						Util.AppendWriteFile(logFile, mes);
						continue;
					}

					// プロセッサを用意
					ConstructorInfo ci = ProcessorType.GetConstructor(new Type[]{typeof(EcmProject)});
					if(ci == null) throw new Exception(ProcessorType.Name + "には、EcmProject を引数に持つコンストラクタがありません。");
					Object o = ci.Invoke(new Object[]{myProject});
					EcmProcessorBase p = o as EcmProcessorBase;
					// information は記録しない
					p.Log.MinimumErrorLevel = EcmErrorLevel.Important;


					string parseStartMes = string.Format("{0} : {1} {2}処理開始\n", DateTime.Now, i, ProcessorType.Name);
					ProcessResult pr = p.Process(i);
					Interlocked.Increment(ref myCounter);
					Util.AppendWriteFile(logFile, parseStartMes);
					Util.AppendWriteFile(logFile, p.Log.ToString());
					if(p.Log.ErrorLevel < EcmErrorLevel.Error){
						string parseEndMes = string.Format("{0} : {1} 処理終了\n", DateTime.Now, i);
						Util.AppendWriteFile(logFile, parseEndMes);
					}
					if(!String.IsNullOrEmpty(pr.Message)){
						Util.AppendWriteFile(logFile, string.Format("{0} : {1}\n", i, pr.Message));
						Util.AppendWriteFile(logFile, "\n");
					}
					foreach(string errroMes in pr.Errors){
						Util.AppendWriteFile(logFile, string.Format("{0} : [Error] {1}\n", i, errroMes));
						Util.AppendWriteFile(logFile, "\n");
					}
				}
				Util.AppendWriteFile(logFile, myProject.Id + " : 一括処理が完了しました。");
			} catch(ThreadAbortException) {
				Util.AppendWriteFile(logFile, "処理は強制終了されました。");
			} catch(Exception e) {
				Util.AppendWriteFile(logFile, e.ToString());
			} finally {
				File.Copy(logFile, resultFile, true);
				File.Delete(logFile);
				myProcessorHandler.Set();
			}
		}


		// 一括publish中に進捗を書き出すためのテンポラリログファイル名を取得します。
		// このファイルは全プロジェクトで共通です。
		private string GetTempLogName(string s){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("プロジェクトディレクトリが存在しません : " + logDir);
			return logDir.TrimEnd('\\') + "\\" + s + "_allprocess.temp";
		}

		// 最終的に書き出すログファイル名を取得します。
		// このディレクトリは各プロジェクトごとに異なります。
		private string GetResultLogName(string s){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("プロジェクトディレクトリが存在しません : " + logDir);
			return logDir.TrimEnd('\\') + "\\" + s + "_allprocess.log";
		}

		// 指定されたの名前のプロセッサを取得します。
		private Type GetProcessorType(string s){
			foreach(Type t in Setting.ProcessorTypes){
				if(s.Equals(t.Name, StringComparison.CurrentCultureIgnoreCase)) return t;
			}
			return null;
		}
	}
}


