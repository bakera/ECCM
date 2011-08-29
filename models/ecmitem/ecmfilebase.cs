using System;
using System.IO;
using System.Text;


namespace Bakera.Eccm{
	public abstract class EcmFileBase : EcmString{


		protected FileInfo myFile;
		protected string myData = null;
		protected string myPath;

// コンストラクタ

		public EcmFileBase(string path, EcmProject project) : this(project){
			this.Path = path;
		}

		public EcmFileBase(EcmProject project) : base(project){
			myProject = project;
		}



// プロパティ
		// パスを取得します。
		public virtual string Path{
			get{ return myPath; }
			private set{ myPath = value.Replace("\\", "/"); }
		}

		// この EcmItem に対応するファイルを示す FileInfo を取得します。
		public virtual FileInfo File{
			get{
				if(this.Path == null) return null;
				if(myFile == null) myFile = new FileInfo(PathToFilename(this.Path));
				return myFile;
			}
		}

		// この EcmItem に対応するファイルの有無を取得します。
		public virtual bool Exists{
			get{
				if(this.File == null) return false;
				return this.File.Exists;
			}
		}

		// ファイルのフルパスを取得します。
		public string FilePath{
			get{
				if(this.File == null) return null;
				return this.File.FullName;
			}
		}

		public long FileSize{
			get{
				if(this.File == null) return 0;
				if(!this.File.Exists) return 0;
				return this.File.Length;
			}
		}

		// ファイルサイズを短縮形で示す示す文字列を取得します。ファイルがないときは "-" を返します。
		public string FileSizeShort{
			get{
				long size = FileSize;
				if(size == 0) return "-";
				if(size < 1024) return size.ToString();
				return String.Format("{0}K", size/1024);
			}
		}

		// 1000 刻みで単位の変わる、ファイルサイズを示す文字列を取得します。
		public string FileSize1000{
			get{
				long size = FileSize;
				if(size < 1000) return size.ToString();
				if(size < 1000000 )return String.Format("{0}KB", size/1000);
				return String.Format("{0}MB", size/1000000);
			}
		}

		// 1024 刻みで単位の変わる、ファイルサイズを示す文字列を取得します。
		public string FileSize1024{
			get{
				long size = FileSize;
				if(size < 1024) return size.ToString();
				if(size < 1024 * 1024 )return String.Format("{0}KB", size/1024);
				return String.Format("{0}MB", size/(1024 * 1024));
			}
		}

		// ファイルサイズの数値をキロバイトで取得します。
		public long FileSizeKB{
			get{
				decimal size = (decimal)FileSize / 1000m;
				return (long)Math.Ceiling(size);
			}
		}
		// ファイルサイズの数値をキビバイトで取得します。
		public long FileSizeKiB{
			get{
				decimal size = (decimal)FileSize / 1024m;
				return (long)Math.Ceiling(size);
			}
		}

		// ファイルサイズの数値をメガバイトで取得します。
		public long FileSizeMB{
			get{
				decimal size = (decimal)FileSize / 1000000m;
				return (long)Math.Ceiling(size);
			}
		}
		// ファイルサイズの数値をメビバイトで取得します。
		public long FileSizeMiB{
			get{
				decimal size = (decimal)FileSize / (1024m*1024m);
				return (long)Math.Ceiling(size);
			}
		}


// メソッド

		// Project.CurrentItem からの相対URIを取得します。
		public string RelUri(){
			EcmFileBase target = Project.CurrentItem;
			if(target == this) return "";

			Uri targetUri = new Uri(Project.Setting.PreviewRootUrl + this.Path);
			Uri fromUri = new Uri(Project.Setting.PreviewRootUrl + Project.CurrentItem.Path);
			if(targetUri.AbsoluteUri == fromUri.AbsoluteUri) return "";

			Uri resultUri = fromUri.MakeRelativeUri(targetUri);
			string result = resultUri.ToString();
			if(Project.Setting.IndexLinkSuffix != null){
				result = Util.CutRight(result, Project.Setting.IndexLinkSuffix);
			}
			// ここまで来て結果が空の場合、"./" の解釈に失敗している
			if(result == "") return "./";
			return result;
		}

		public string RelUrl(){
			return RelUri();
		}

		// 任意の文字列を相対URIに変換します。
		public string RelUri(string s){

			Uri targetUri = new Uri(Project.Setting.PreviewRootUrl + s);
			Uri fromUri = new Uri(Project.Setting.PreviewRootUrl + this.Path);

			if(targetUri.AbsoluteUri == fromUri.AbsoluteUri) return "";

			Uri resultUri = fromUri.MakeRelativeUri(targetUri);
			string result = resultUri.ToString();
			if(!string.IsNullOrEmpty(Project.Setting.IndexLinkSuffix)){
				result = Util.CutRight(result, Project.Setting.IndexLinkSuffix);
			}
			// ここまで来て結果が空の場合、"./" の解釈に失敗している
			if(result == "") return "./";
			return result;
		}
		public string RelUrl(string s){
			return RelUri(s);
		}

		public string ReadContent(){
			if(myData == null) LoadData();
			return myData;
		}

		public void LoadData(){
			if(!this.File.Exists){
				throw new Exception("ファイルをロードしようとしましたが、ファイルが見つかりません : " + this.File.FullName);
			}
			myData = Util.LoadFile(this.File.FullName, myProject.Setting.HtmlEncodingObj);
		}

		// ファイルにコンテンツを書き込みます。
		public void WriteContent(string s){
			if(!this.File.Exists){
				throw new Exception("ファイルに書き込みしようとしましたが、ファイルが見つかりません : " + this.File.FullName);
			}
			Util.WriteFile(this.FilePath, s, myProject.Setting.HtmlEncodingObj);
		}

		// 与えられたパスに相当するファイル名 (サーバ内のフルパス名) を取得します。
		// URL の場合はスキームとドメイン名部分を削除して処理します。
		public string PathToFilename(string path){
			if(path == null) path = "";

			string result = myProject.Setting.DocumentFullPath.FullName.TrimEnd('/') + '/' + path.TrimStart('/');
			return result;
		}

	}
}

