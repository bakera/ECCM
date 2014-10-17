using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Serialization;


namespace Bakera.Eccm{


	// プラグインの状態を表示します。
	public class PluginManager : EcmList{

		public PluginManager(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "プラグイン";
		public new const string PathName = "plugins";
		private FileInfo myTargetSrc;
		private static Compiler myCompiler = new Compiler();


// プロパティ
		public override string SubTitle{
			get{return Name;}
		}


// Get/Post メソッド

		// 引数がないとき : 特定プロジェクトのプレビュー画像一覧を表示します。
		// ID が渡されたとき : その ID のプレビュー画像を返します。
		public override EcmResponse Get(HttpRequest rq){
			string testSrcId = GetSrcId(rq);

			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			if(string.IsNullOrEmpty(testSrcId)){
				result.AppendChild(ViewList());
			} else {
				result.AppendChild(ViewSrc(testSrcId));
			}
			return new HtmlResponse(myXhtml, result);
		}


		// POST 
		public override EcmResponse Post(HttpRequest rq){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string testSrcId = GetSrcId(rq);
			if(string.IsNullOrEmpty(testSrcId)){
				result.AppendChild(CompileAll());
			} else {
				result.AppendChild(CompileTest(testSrcId));
			}
			return new HtmlResponse(myXhtml, result);
		}


// private メソッド

// 一覧表示
		// 一覧を表示します (デフォルト)
		private XmlNode ViewList(){

			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			if(myProject.Setting.PluginKind == PluginKind.None){
				result.AppendChild(myXhtml.P(null, "プラグインの言語が指定されていないため、プラグインを使用できません。プラグインを使用するには、「設定」から「プラグインの言語」の設定を行ってください。"));
			} else {
				result.AppendChild(ViewPluginType());
				result.AppendChild(ViewPlugins());
				result.AppendChild(ViewFiles());
				result.AppendChild(ViewCompilerState());
				result.AppendChild(ViewTestFiles());
			}
			return result;
		}

		// コンパイル済みプラグインの情報を表示します。
		private XmlNode ViewPluginType(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			result.AppendChild(myXhtml.P(null, "プラグインの言語 : ", myProject.Setting.PluginKind));
			return result;
		}

		// コンパイル済みプラグインの情報を表示します。
		private XmlNode ViewPlugins(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			Type[] typeList = myProject.GetPlugins();
			XmlElement h2 = myXhtml.H(2, null, "コンパイル済みプラグイン");
			result.AppendChild(h2);

			if(myProject.BinDirectory == null){
				result.AppendChild(myXhtml.P("note", "プラグイン DLL ファイル実体のパスを辿れません。web.config に binディレクトリの情報が正しく設定されているか確認してください。"));
			} else if(myProject.PluginAssembly == null){
				result.AppendChild(myXhtml.P("note", "プラグインはありません。"));
			} else {
				FileInfo f = myProject.GetPluginAsmFile();
				AssemblyName an = myProject.PluginAssembly.GetName();
				result.AppendChild(myXhtml.P(null, string.Format("{0} : {1}", f.Name, f.LastWriteTime)));
			}

			if(typeList.Length > 0){
				XmlElement ul = myXhtml.Create("ul");
				foreach(Type t in typeList){
					XmlElement li = myXhtml.Create("li");
					li.InnerText = t.Name;
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
			} else {
				XmlElement p = myXhtml.P("note", "プラグインはありません。");
				result.AppendChild(p);
			}
			return result;
		}

		// コンパイル済みプラグインの情報を表示します。
		private XmlNode ViewCompilerState(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			XmlElement h2 = myXhtml.H(2, null, "コンパイラの参照アセンブリ");
			result.AppendChild(h2);
			foreach(string s in myCompiler.CurrentReferenceAsmNames){
				result.AppendChild(myXhtml.P(null, s));
			}

			return result;
		}


		// ソースファイルの一覧を表示します。
		private XmlNode ViewFiles(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			XmlElement h2 = myXhtml.H(2, null, "プラグインソースファイル一覧");
			result.AppendChild(h2);

			FileInfo[] files = myProject.GetSrcFiles("*");
			if(files.Length == 0){
				result.AppendChild(myXhtml.P("note", "プラグインソースファイルはありません。"));
			} else {
				XmlElement ul = myXhtml.Create("ul");
				foreach(FileInfo f in files){
					XmlElement li = myXhtml.Create("li");
					li.AppendChild(ViewFileInfo(f));
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
				result.AppendChild(CompileButton());
			}
			return result;
		}

		// コンパイルボタンを表示します。
		private XmlNode CompileButton(){
			XmlElement form = myXhtml.Form(null, "post");
			XmlElement submitP = myXhtml.P("submit", myXhtml.CreateSubmit("コンパイル"));
			form.AppendChild(submitP);
			return form;
		}

		// テストファイルの一覧を表示します。
		private XmlNode ViewTestFiles(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			XmlElement h2 = myXhtml.H(2, null, "プラグインテストファイル一覧");
			result.AppendChild(h2);

			FileInfo[] files = myProject.GetTestFiles("*");
			if(files.Length == 0){
				result.AppendChild(myXhtml.P("note", "プラグインテストファイルはありません。"));
			} else {
				XmlElement ul = myXhtml.Create("ul");
				foreach(FileInfo f in files){
					XmlElement li = myXhtml.Create("li");
					li.AppendChild(ViewFileInfo(f));
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
			}
			return result;
		}


// 詳細表示
		private XmlNode ViewSrc(string srcId){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			FileInfo[] files = myProject.GetFiles(srcId);
			if(files.Length == 0){
				string mes = string.Format("プラグインファイル {0} がみつかりません。", srcId);
				return myXhtml.P(null, mes);
			}
			myTargetSrc = files[0];
			result.AppendChild(ViewFileInfo(myTargetSrc));

			string srcData = null;
			using(FileStream fs = myTargetSrc.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs)){
					srcData = sr.ReadToEnd();
					sr.Close();
				}
				fs.Close();
			}
			XmlElement pre = Html.Create("pre", "source");
			pre.InnerText = srcData;
			result.AppendChild(pre);

			XmlElement form = myXhtml.Form(null, "post");
			XmlElement submitP = myXhtml.P("submit", myXhtml.CreateSubmit("テスト"));
			form.AppendChild(submitP);
			result.AppendChild(form);
			return result;
		}



		private XmlNode ViewFileInfo(FileInfo f){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string hrefStr = PluginManager.PathName + '/' + Util.CutRight(f.Name, myProject.PluginSourceSuffix);
			string info = string.Format(" {0} ({1}bytes)", f.LastWriteTime, f.Length);

			if(f == myTargetSrc){
				result.AppendChild(myXhtml.Text(f.Name));
			} else {
				XmlElement a = myXhtml.Create("a");
				a.InnerText = f.Name;
				a.SetAttribute("href", hrefStr);
				result.AppendChild(a);
			}
			result.AppendChild(myXhtml.Text(info));
			return result;
		}

// コンパイル

		// すべてのソースファイルをコンパイルします。
		private XmlNode CompileAll(){
			FileInfo[] files = myProject.GetSrcFiles("*");
			if(files.Length == 0){
				string mes = "プラグインソースファイルがみつかりません。";
				return myXhtml.P(null, mes);
			}

			FileInfo asmFile = myProject.GetPluginAsmFile();
			CompilerResults cr = myCompiler.Compile(files, asmFile, myProject);
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			if(cr.NativeCompilerReturnValue == 0){
				result.AppendChild(myXhtml.H(2, null, "コンパイル成功"));
				result.AppendChild(myXhtml.P(null, "プラグインのコンパイルに成功しました。"));
			} else {
				result.AppendChild(myXhtml.H(2, null, "コンパイルエラー"));
				result.AppendChild(myXhtml.P(null, "プラグインのコンパイルに失敗しました。"));
			}
			if(cr.Errors.HasErrors){
				foreach(CompilerError ce in cr.Errors){
					result.AppendChild(myXhtml.P(null, ce));
				}
				return result;
			}
			if(cr.Output.Count > 0){
				XmlElement ol = myXhtml.Create("ol");
				foreach(string s in cr.Output){
					ol.AppendChild(myXhtml.Create("li", null, s));
				}
				result.AppendChild(ol);
			}
			return result;
		}


		// コンパイルのテストを行います。
		private XmlNode CompileTest(string srcId){
			FileInfo[] files = myProject.GetFiles(srcId);
			if(files.Length == 0){
				string mes = string.Format("プラグインファイル {0} がみつかりません。", srcId);
				return myXhtml.P(null, mes);
			}
			myTargetSrc = files[0];
			CompilerResults cr = myCompiler.Compile(myTargetSrc, null, myProject);
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			if(cr.NativeCompilerReturnValue == 0){
				result.AppendChild(myXhtml.H(2, null, "コンパイル成功"));
				result.AppendChild(myXhtml.P(null, srcId, " のコンパイルに成功しました。"));
//				Assembly compiledAsm = cr.CompiledAssembly;
			} else {
				result.AppendChild(myXhtml.H(2, null, "コンパイルエラー"));
				result.AppendChild(myXhtml.P(null, srcId, " のコンパイルに失敗しました。"));
			}
			if(cr.Errors.HasErrors){
				foreach(CompilerError ce in cr.Errors){
					result.AppendChild(myXhtml.P(null, ce));
				}
				return result;
			}
			if(cr.Output.Count > 0){
				XmlElement ol = myXhtml.Create("ol");
				foreach(string s in cr.Output){
					ol.AppendChild(myXhtml.Create("li", null, s));
				}
				result.AppendChild(ol);
			}

			XmlElement form = myXhtml.Form(null, "post");
			XmlElement submitP = myXhtml.P("submit", myXhtml.CreateSubmit("再テスト"));
			form.AppendChild(submitP);
			result.AppendChild(form);

			return result;
		}


		private string GetSrcId(HttpRequest rq){
			string thisPath = "/" + myProject.Id + "/" + PathName;
			return Util.CutLeft(rq.PathInfo, thisPath).Trim('/');
		}

	}
}


