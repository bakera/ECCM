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

			// プロジェクトデータのキャッシュがあり、最新ならそれを返す
			// データファイルが最新、かつ設定が最新であればキャッシュを使用してよい
			EcmProject result = GetProjectCache(projectId);
			if(result != null) return result;

			// キャッシュが使えない場合
			Setting s = GetSetting(projectId);
			if(s == null) return null;
			EcmProject newResult = new EcmProject(this, s);
			myProjectList[projectId] = newResult;
			return newResult;
		}


		// プロジェクトデータの有効なキャッシュがあれば返します。
		// データが最新、かつ設定が最新であればキャッシュを使用して良いと判断します。
		// キャッシュが使用できない場合は null を返します。
		public EcmProject GetProjectCache(string projectId){
			// キャッシュが存在するか?
			if(!myProjectList.ContainsKey(projectId)) return null;
			EcmProject result = myProjectList[projectId];
			if(result == null) return null;

			// データが最新か?
			// データファイルの更新日がデータの更新日より新しい場合はキャッシュ使用不可
			if(result.FileTime == default(DateTime)) return null;
			if(result.FileTime >= result.DataTime) return null;

			// 設定ファイルが最新か?
			// 設定ファイルの更新日がデータの更新日より新しい場合はキャッシュ使用不可
			result.Setting.BaseFile.Refresh();
			if(result.Setting.BaseFile.LastWriteTime >= result.DataTime) return null;
			
			
			return result;
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




