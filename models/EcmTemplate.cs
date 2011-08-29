using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bakera.Eccm{
	public class EcmTemplate{

// コンストラクタ
		private Setting mySetting;
		private EcmProject myProject;
		private string myName;
		private FileInfo myFile;
		private FileInfo myBackupFile;
		private const string TemplateBackupDirName = "template_backup";

		public EcmTemplate(EcmProject proj, string templateName){
			myProject = proj;
			mySetting = proj.Setting;
			myName = templateName;

			string templateFileName = mySetting.TemplateFullPath.FullName.TrimEnd('\\') + '\\' + templateName + '.' + mySetting.TemplateExt.TrimStart('.');
			myFile = new FileInfo(templateFileName);
			myBackupFile = new FileInfo(GetBackupPath());
		}


// プロパティ
		public string Name{
			get{return myName;}
		}

		public FileInfo File{
			get{return myFile;}
		}

		public bool Exists{
			get{return myFile.Exists;}
		}

// メソッド

		// データをそのまま取得します。
		public string GetData(){
			if(!myFile.Exists) return null;
			string result = Util.LoadFile(myFile.FullName, mySetting.HtmlEncodingObj);
			return result;
		}

		// バックアップファイルを調査し、必要なバックアップを作成します。
		public void Backup(){
			FileInfo currentBackupFile = new FileInfo(GetBackupPath());
			// なかったらコピーしておく
			if(!currentBackupFile.Exists){
				try{
					currentBackupFile.Directory.Create();
					myFile.CopyTo(currentBackupFile.FullName, true);
				}catch{}
				return;
			}

			// バックアップファイルがあるのでタイムスタンプを比較
			// 同じなら何もしない
			if(File.LastWriteTime == currentBackupFile.LastWriteTime) return;

			// 前回バックアップを更新
			FileInfo prevBackupFile = new FileInfo(currentBackupFile.FullName + ".prev");
			currentBackupFile.CopyTo(prevBackupFile.FullName, true);

			// 現在バックアップを更新
			myFile.CopyTo(currentBackupFile.FullName, true);
		}


// プライベートメソッド

		// バックアップのパスを取得する
		private string GetBackupPath(){
			string templateFilePath = mySetting.BaseDir.FullName.TrimEnd('\\') + '\\' + TemplateBackupDirName + '\\' + myName + '.' + mySetting.TemplateExt.TrimStart('.');
			return templateFilePath;
		}




	}
}


