using System;
using System.Data;
using System.Xml;
using System.IO;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml.Serialization;


namespace Bakera.Eccm{

	public class Eccm : Page{

		private Xhtml xhtml;
		private string myProjectDir;
		private string myHandlerDir;
		private EcmProject myProject;
		private string[] myHandlerList;

		private static EcmProjectManager myProjectManager = new EcmProjectManager();

		private const string ProjectIdEditName = "projectName";
		private const string ProjectIdRuleDescription = "IDは半角英字で始まり、半角英数字とハイフンのみで指定する必要があります。";
		private readonly static Regex ProjectIdRegex = new Regex("^[a-zA-Z][-0-9a-zA-Z]*$");

		public const string ProjectDirKey = "EccmProject";
		public const string BinDirKey = "EccmBinDirectory";

// プロパティ



// パブリックメソッド
		public void Page_Load(Object source, EventArgs e) {

			myProjectDir = WebConfigurationManager.AppSettings[ProjectDirKey];
			myHandlerDir = WebConfigurationManager.AppSettings["EccmHandler"];
			string templateFile = WebConfigurationManager.AppSettings["EccmTemplate"];
			if(string.IsNullOrEmpty(templateFile)){
				throw new Exception("テンプレートファイルが設定されていません。web.config で EccmTemplate を設定する必要があります。");
			}
			if(!File.Exists(templateFile)){
				throw new Exception("テンプレートファイルがみつかりませんでした : " + templateFile);
			}
			xhtml = new Xhtml(templateFile);
			xhtml.Title.InnerText = "ECCM";

			// パスがあるかな
			string[] path = Request.PathInfo.Trim('/').Split('/');

			try{
				ProcessProject(path);
			} catch(Exception ex){
				XmlElement alert = xhtml.CreateElement("pre");
				alert.InnerText = ex.ToString();
				xhtml.Body.PrependChild(alert);
				Response.Write(xhtml.OuterXml);
			}
		}



		// 特定のプロジェクトを処理します。
		private void ProcessProject(string[] opt){
			if(opt.Length < 1){
				ProjectList();
				return;
			}
			string projectId = opt[0];
			if(String.IsNullOrEmpty(projectId)){
				ProjectList();
				return;
			}

			if(!Directory.Exists(myProjectDir)){
				ShowError("ディレクトリ {0} がみつかりません。", myProjectDir);
				return;
			}

			string projectXmlFile = GetXmlPath(projectId);
			if(!File.Exists(projectXmlFile)){
				ShowError("プロジェクト {0} はありません。", projectId);
				return;
			}

			myProject = myProjectManager[projectId];
			myHandlerList = GetHandlerList();

			EcmResponse result = null;
			EcmProjectHandler eph = null;

			if(opt.Length > 1){
				string optname = opt[1];
				foreach(Type t in Setting.HandlerTypes){
					string pathname = Util.GetFieldValue(t, "PathName");
					if(optname == pathname){
						ConstructorInfo ci = t.GetConstructor(new Type[]{typeof(EcmProject), typeof(Xhtml)});
						if(ci == null) throw new Exception(optname + "には、EcmProject, Xhtml を引数に持つコンストラクタがありません。");
						Object o = ci.Invoke(new Object[]{myProject, xhtml});
						eph = o as EcmProjectHandler;
						xhtml.Body.SetAttribute("id", myProject.Setting.Id);
						xhtml.Body.SetAttribute("class", optname);
						break;
					}
				}
			}

			if(eph == null){
				eph = new EcmList(myProject, xhtml);
			}

			eph.SetTitle();
			if(Request.HttpMethod.Equals("post", StringComparison.CurrentCultureIgnoreCase)){
				result = eph.Post(Request);
			} else {
				result = eph.Get(Request);
			}

			// HTML ならナビをつける
			if(result is HtmlResponse){
				HtmlResponse hres = result as HtmlResponse;
				hres.BodyXml.Body.PrependChild(GetHandlerNav());
			}

			if(result != null){
				result.WriteResponse(Response);
			}
		}


		// プロジェクトID が指定されていない場合の処理
		private void ProjectList(){
			if(!Directory.Exists(myProjectDir)){
				ShowError("ディレクトリ {0} がみつかりません。", myProjectDir);
				return;
			}

			if(Request.HttpMethod.Equals("post", StringComparison.CurrentCultureIgnoreCase)){
				PostNewProject();
			} else {
				ViewProjectList();
			}
			EcmResponse res = new HtmlResponse(xhtml);
			res.WriteResponse(Response);
		}

		// プロジェクトの一覧を表示します。
		private void ViewProjectList(){

			string[] projectSubDirs = Directory.GetDirectories(myProjectDir);
			var settingList = new List<Setting>();
			foreach(string dirPath in projectSubDirs){
				string projId = Path.GetFileNameWithoutExtension(dirPath);
				string projectXmlFile = GetXmlPath(projId);
				if(!File.Exists(projectXmlFile)) continue;
				Setting s = Setting.GetSetting(projectXmlFile);
				settingList.Add(s);
			}
			settingList.Sort((x, y) => y.CreatedTime.CompareTo(x.CreatedTime));

			if(settingList.Count == 0){
				XmlElement p = xhtml.P();
				p.InnerText = "プロジェクトはありません。(設定ファイルディレクトリ = " + myProjectDir + ")";
				xhtml.Body.AppendChild(p);
			} else {
				XmlElement h1 = xhtml.H(1);
				h1.InnerText = "ECCM管理プロジェクト一覧";

				XmlElement table = xhtml.Create("table", "projectlist");
				XmlElement thead = xhtml.CreateThead("タイトル / ID","作成日", "更新日");
				table.AppendChild(thead);
				foreach(Setting s in settingList){
					string pName = s.ProjectName;
					if(pName == null) pName = "(名称未設定プロジェクト)";

					string fileDate = "-";
					FileInfo dataFile = new FileInfo(s.CsvFullPath);
					if(dataFile.Exists){
						fileDate = string.Format("{0} ({1})", dataFile.LastWriteTime, GetFileSizeString(dataFile.Length));
					}
					XmlElement a = GetLink(string.Format("{0} / {1}", pName, s.Id), s.Id);
					XmlElement tr = xhtml.CreateTr(a, s.CreatedTime, fileDate);

					table.AppendChild(tr);
				}
				xhtml.Body.AppendChild(h1);
				xhtml.Body.AppendChild(table);
			}

			XmlNode form = GetNewProjectForm();
			xhtml.Body.AppendChild(form);
		}

		private XmlNode GetNewProjectForm(){
			XmlElement form = xhtml.Create("form");
			form.SetAttribute("action", "");
			form.SetAttribute("method", "post");

			XmlElement addH = xhtml.H(2, null, "新規プロジェクトの追加");
			form.AppendChild(addH);

			XmlElement desc = xhtml.P("desc", "新規プロジェクトを追加する場合は、ID を入力して「新規追加」ボタンを押してください。" + ProjectIdRuleDescription);
			form.AppendChild(desc);

			XmlNode input = xhtml.Input(ProjectIdEditName, null, "プロジェクト ID");
			XmlElement btn = xhtml.CreateSubmit("新規追加");

			XmlNode ed = xhtml.P("edit", input, btn);
			form.AppendChild(ed);

			return form;
		}

		private string GetFileSizeString(long size){
			if(size < 1000) return size.ToString();
			return String.Format("{0:n0}KB", size/1000);
		}


		// 新規プロジェクトを追加します。
		private void PostNewProject(){
			string projName = Request.Form[ProjectIdEditName];
			
			if(string.IsNullOrEmpty(projName)){
				ShowError("プロジェクト ID が指定されていません。");
				XmlNode form = GetNewProjectForm();
				xhtml.Body.AppendChild(form);
				return;
			}
			if(!ProjectIdRegex.IsMatch(projName)){
				ShowError("プロジェクト ID [{0}]は正しくありません。" + ProjectIdRuleDescription, projName);
				XmlNode form = GetNewProjectForm();
				xhtml.Body.AppendChild(form);
				return;
			}

			FileInfo fi = new FileInfo(GetXmlPath(projName));
			if(fi.Exists){
				ShowError("プロジェクト ID [{0}]は既に使用されています。", projName);
				XmlNode form = GetNewProjectForm();
				xhtml.Body.AppendChild(form);
				return;
			}

			if(!fi.Directory.Exists){
				fi.Directory.Create();
			}


			Setting newSetting =  new Setting();
			XmlSerializer xs = new XmlSerializer(typeof(Setting));
			using(FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.None)){
				using(StreamWriter sw = new StreamWriter(fs)){
					xs.Serialize(fs, newSetting);
					sw.Close();
				}
				fs.Close();
			}

			XmlElement a = GetLink(projName + "のページ", projName);
;
			XmlElement p = xhtml.P(null, "プロジェクト [" + projName + "] を作成しました。", a);
			xhtml.Body.AppendChild(p);
		}


		// メニューに表示するハンドラの型名のリストを取得します。
		private string[] GetHandlerList(){
			string[] handlerList = new string[0];
			if(!string.IsNullOrEmpty(myProject.Setting.Handler)){
				handlerList = myProject.Setting.Handler.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
			}
			for(int i=0; i < handlerList.Length; i++){
				handlerList[i] = handlerList[i].Trim();
			}
			return handlerList;
		}


		// EcmProjectHandler の各機能へのリンクを生成します。
		private XmlNode GetHandlerNav(){
			XmlElement nav = xhtml.P("nav");

			// トップ
			nav.AppendChild(xhtml.CreateTextNode("["));
			nav.AppendChild(GetLink("ECCM", null));
					nav.AppendChild(xhtml.CreateTextNode("]"));

			// その他
			if(myHandlerList.Length == 0){
				foreach(Type t in Setting.HandlerTypes){
					nav.AppendChild(xhtml.CreateTextNode(" | "));
					nav.AppendChild(GetHandlerNav(t));
				}
			} else {
				// 一覧がなければ足す
				if(Array.IndexOf(myHandlerList, "EcmList") < 0){
					nav.AppendChild(xhtml.CreateTextNode(" | "));
					nav.AppendChild(GetHandlerNav(typeof(EcmList)));
				}

				// 設定がなければ足す
				if(Array.IndexOf(myHandlerList, "ProjectSetting") < 0){
					nav.AppendChild(xhtml.CreateTextNode(" | "));
					nav.AppendChild(GetHandlerNav(typeof(ProjectSetting)));
				}

				foreach(Type t in Setting.HandlerTypes){
					foreach(string s in myHandlerList){
						if(Array.IndexOf(myHandlerList, t.Name) >= 0){
							nav.AppendChild(xhtml.CreateTextNode(" | "));
							nav.AppendChild(GetHandlerNav(t));
							break;
						}
					}
				}
			}
			return nav;
		}

		private XmlNode GetHandlerNav(Type t){
			string name = Util.GetFieldValue(t, "Name");
			string projectId = myProject.Id;
			string pathName = Util.GetFieldValue(t, "PathName");
			return GetLink(name, projectId, pathName);
		}


		private XmlElement GetLink(string text, params string[] s){
			string target = GetUrl(s);
			if(target == Request.Path && Request.HttpMethod.ToLower() == "get"){
				XmlElement em = xhtml.Create("em");
				em.SetAttribute("class", "stay");
				em.InnerText = text;
				return em;
			}
			XmlElement a = xhtml.Create("a");
			a.SetAttribute("href", target);
			a.InnerText = text;
			return a;
		}

		private string GetUrl(params string[] path){
			string result = "";
			if(path != null){
				foreach(string s in path){
					if(s == null) continue;
					result += "/" + s;
				}
			}
			return Request.FilePath + result;
		}

		private void ShowError(string format, params string[] strs){
			ShowError(String.Format(format, strs));
		}

		private void ShowError(string s){
			XmlElement p = xhtml.P();
			p.InnerText = s;
			if(myProject != null) myProject.Log.AddError(s);
			xhtml.Body.AppendChild(p);
			Response.Write(xhtml.OuterXml);
		}


		// プロジェクトIDから、対応するプロジェクト設定 XML ファイル名を取得します。
		private string GetXmlPath(string id){
			string dir = myProjectDir.TrimEnd('/', '\\');
			return string.Format("{0}\\{1}\\{1}.xml", dir, id);
		}

	}

}



