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
		private const string ProjectIdRuleDescription = "ID�͔��p�p���Ŏn�܂�A���p�p�����ƃn�C�t���݂̂Ŏw�肷��K�v������܂��B";
		private readonly static Regex ProjectIdRegex = new Regex("^[a-zA-Z][-0-9a-zA-Z]*$");

		public const string ProjectDirKey = "EccmProject";
		public const string BinDirKey = "EccmBinDirectory";

// �v���p�e�B



// �p�u���b�N���\�b�h
		public void Page_Load(Object source, EventArgs e) {

			myProjectDir = WebConfigurationManager.AppSettings[ProjectDirKey];
			myHandlerDir = WebConfigurationManager.AppSettings["EccmHandler"];
			string templateFile = WebConfigurationManager.AppSettings["EccmTemplate"];
			if(string.IsNullOrEmpty(templateFile)){
				throw new Exception("�e���v���[�g�t�@�C�����ݒ肳��Ă��܂���Bweb.config �� EccmTemplate ��ݒ肷��K�v������܂��B");
			}
			if(!File.Exists(templateFile)){
				throw new Exception("�e���v���[�g�t�@�C�����݂���܂���ł��� : " + templateFile);
			}
			xhtml = new Xhtml(templateFile);
			xhtml.Title.InnerText = "ECCM";

			// �p�X�����邩��
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



		// ����̃v���W�F�N�g���������܂��B
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
				ShowError("�f�B���N�g�� {0} ���݂���܂���B", myProjectDir);
				return;
			}

			string projectXmlFile = GetXmlPath(projectId);
			if(!File.Exists(projectXmlFile)){
				ShowError("�v���W�F�N�g {0} �͂���܂���B", projectId);
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
						if(ci == null) throw new Exception(optname + "�ɂ́AEcmProject, Xhtml �������Ɏ��R���X�g���N�^������܂���B");
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

			// HTML �Ȃ�i�r������
			if(result is HtmlResponse){
				HtmlResponse hres = result as HtmlResponse;
				hres.BodyXml.Body.PrependChild(GetHandlerNav());
			}

			if(result != null){
				result.WriteResponse(Response);
			}
		}


		// �v���W�F�N�gID ���w�肳��Ă��Ȃ��ꍇ�̏���
		private void ProjectList(){
			if(!Directory.Exists(myProjectDir)){
				ShowError("�f�B���N�g�� {0} ���݂���܂���B", myProjectDir);
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

		// �v���W�F�N�g�̈ꗗ��\�����܂��B
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
				p.InnerText = "�v���W�F�N�g�͂���܂���B(�ݒ�t�@�C���f�B���N�g�� = " + myProjectDir + ")";
				xhtml.Body.AppendChild(p);
			} else {
				XmlElement h1 = xhtml.H(1);
				h1.InnerText = "ECCM�Ǘ��v���W�F�N�g�ꗗ";

				XmlElement table = xhtml.Create("table", "projectlist");
				XmlElement thead = xhtml.CreateThead("�^�C�g�� / ID","�쐬��", "�X�V��");
				table.AppendChild(thead);
				foreach(Setting s in settingList){
					string pName = s.ProjectName;
					if(pName == null) pName = "(���̖��ݒ�v���W�F�N�g)";

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

			XmlElement addH = xhtml.H(2, null, "�V�K�v���W�F�N�g�̒ǉ�");
			form.AppendChild(addH);

			XmlElement desc = xhtml.P("desc", "�V�K�v���W�F�N�g��ǉ�����ꍇ�́AID ����͂��āu�V�K�ǉ��v�{�^���������Ă��������B" + ProjectIdRuleDescription);
			form.AppendChild(desc);

			XmlNode input = xhtml.Input(ProjectIdEditName, null, "�v���W�F�N�g ID");
			XmlElement btn = xhtml.CreateSubmit("�V�K�ǉ�");

			XmlNode ed = xhtml.P("edit", input, btn);
			form.AppendChild(ed);

			return form;
		}

		private string GetFileSizeString(long size){
			if(size < 1000) return size.ToString();
			return String.Format("{0:n0}KB", size/1000);
		}


		// �V�K�v���W�F�N�g��ǉ����܂��B
		private void PostNewProject(){
			string projName = Request.Form[ProjectIdEditName];
			
			if(string.IsNullOrEmpty(projName)){
				ShowError("�v���W�F�N�g ID ���w�肳��Ă��܂���B");
				XmlNode form = GetNewProjectForm();
				xhtml.Body.AppendChild(form);
				return;
			}
			if(!ProjectIdRegex.IsMatch(projName)){
				ShowError("�v���W�F�N�g ID [{0}]�͐���������܂���B" + ProjectIdRuleDescription, projName);
				XmlNode form = GetNewProjectForm();
				xhtml.Body.AppendChild(form);
				return;
			}

			FileInfo fi = new FileInfo(GetXmlPath(projName));
			if(fi.Exists){
				ShowError("�v���W�F�N�g ID [{0}]�͊��Ɏg�p����Ă��܂��B", projName);
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

			XmlElement a = GetLink(projName + "�̃y�[�W", projName);
;
			XmlElement p = xhtml.P(null, "�v���W�F�N�g [" + projName + "] ���쐬���܂����B", a);
			xhtml.Body.AppendChild(p);
		}


		// ���j���[�ɕ\������n���h���̌^���̃��X�g���擾���܂��B
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


		// EcmProjectHandler �̊e�@�\�ւ̃����N�𐶐����܂��B
		private XmlNode GetHandlerNav(){
			XmlElement nav = xhtml.P("nav");

			// �g�b�v
			nav.AppendChild(xhtml.CreateTextNode("["));
			nav.AppendChild(GetLink("ECCM", null));
					nav.AppendChild(xhtml.CreateTextNode("]"));

			// ���̑�
			if(myHandlerList.Length == 0){
				foreach(Type t in Setting.HandlerTypes){
					nav.AppendChild(xhtml.CreateTextNode(" | "));
					nav.AppendChild(GetHandlerNav(t));
				}
			} else {
				// �ꗗ���Ȃ���Α���
				if(Array.IndexOf(myHandlerList, "EcmList") < 0){
					nav.AppendChild(xhtml.CreateTextNode(" | "));
					nav.AppendChild(GetHandlerNav(typeof(EcmList)));
				}

				// �ݒ肪�Ȃ���Α���
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


		// �v���W�F�N�gID����A�Ή�����v���W�F�N�g�ݒ� XML �t�@�C�������擾���܂��B
		private string GetXmlPath(string id){
			string dir = myProjectDir.TrimEnd('/', '\\');
			return string.Format("{0}\\{1}\\{1}.xml", dir, id);
		}

	}

}



