using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Configuration;

namespace Bakera.Eccm{
	public class EcmProjectManager{

		private string myProjectDir;
		private static Dictionary<string, EcmProject> myProjectList = new Dictionary<string, EcmProject>();

// コンストラクタ

		public EcmProjectManager(){
			myProjectDir = WebConfigurationManager.AppSettings[Eccm.ProjectDirKey];
		}

// プロパティ

		public string ProjectDir{
			get{return myProjectDir;}
			set{myProjectDir = value;}
		}

// インデクサ

		public EcmProject this[string projectId]{
			get{
				return GetProject(projectId);
			}
		}


// メソッド

		// 渡された ID に対応する Setting を取得します。
		public Setting GetSetting(string projectId){
			string projectXmlFile = GetXmlPath(projectId);
			if(!File.Exists(projectXmlFile)){
				return null;
			}
			Setting s = Setting.GetSetting(projectXmlFile);
			return s;
		}

		// 渡された ID に対応する EcmProject を取得します。
		public EcmProject GetProject(string projectId){
			// データがあって、最新ならそれを返す
			if(myProjectList.ContainsKey(projectId)){
				EcmProject result = myProjectList[projectId];
				if(result != null && result.FileTime != default(DateTime) && result.FileTime == result.DataTime) return result;
			}
			Setting s = GetSetting(projectId);
			if(s == null) return null;
			EcmProject newResult = new EcmProject(this, s);
			myProjectList[projectId] = newResult;
			return newResult;
		}

		// ロードされている全ての EcmProject を取得します。
		public EcmProject[] GetAllProject(){
			EcmProject[] result = new EcmProject[myProjectList.Values.Count];
			myProjectList.Values.CopyTo(result, 0);
			return result;
		}

		// プロジェクトディレクトリを検索し、全ての EcmProject をロードします。
		public void LoadAllProject(){
			string[] projectSubDirs = Directory.GetDirectories(myProjectDir);
			if(projectSubDirs.Length == 0) return;
			foreach(string dirPath in projectSubDirs){
				string projId = Path.GetFileNameWithoutExtension(dirPath);
				Setting s = this.GetSetting(projId);
				if(s == null) continue;
				GetProject(projId);
			}
		}


		// プロジェクトIDから、対応するプロジェクト設定 XML ファイル名を取得します。
		private string GetXmlPath(string id){
			string dir = myProjectDir.TrimEnd('/', '\\');
			return string.Format("{0}\\{1}\\{1}.xml", dir, id);
		}


	}
}




