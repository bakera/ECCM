using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;


namespace Bakera.Eccm{
	public class EcmItem : EcmFileBase{
		private static TimeSpan OneMinute = TimeSpan.FromMinutes(1);
		private static TimeSpan OneHour = TimeSpan.FromHours(1);
		private static TimeSpan OneDay = TimeSpan.FromDays(1);

		private DataRow myRow = null;

		// 予約名
		public const string ReservedMark = "#";
		public const string ReservedIdName = "id";
		public const string ReservedTitleName = "title";
		public const string ReservedPathName = "path";
		public const string ReservedTemplateName = "template";
		public const string ReservedParentName = "parent";
		public const string ReservedSchemaName = "schema";

		public const string NavStayTemplate = "<li class=\"stay\"><em>{0}</em></li>";
		public const string NavLinkTemplate = "<li><a href=\"{0}\">{1}</a></li>";
		public const string AncestorNavLinkTemplate = "<li class=\"stay\"><a href=\"{0}\">{1}</a></li>";
		public const string NavDisabledTemplate = "<li class=\"disabled\"><span>{0}</span></li>";

// コンストラクタ

		// ID と DataRow から EcmItem を作成します。
		public EcmItem(string id, DataRow row) : base(row.Table as EcmProject){
			myId = id;
			myRow = row;
			myPath = Project.GetPathByRow(row);
		}

		// DataRow のみから EcmItem を作成します。
		public EcmItem(DataRow row) : base(row.Table as EcmProject){
			myRow = row;
			Id = Project.GetIdByRow(row);
			myPath = Project.GetPathByRow(row);
		}

// 比較演算子のオーバーライド
		public override bool Equals(Object o){
			EcmItem targetItem = o as EcmItem;
			if(targetItem == null) return false;
			return FqId.Equals(targetItem.FqId);
		}

		public override int GetHashCode(){
			return FqId.GetHashCode();
		}

		public static bool operator ==(EcmItem a, EcmItem b){
			if (Object.ReferenceEquals(a, b)) return true;
			if (((Object)a == null) || ((Object)b == null)) return false;
			return a.Equals(b);
		}

		public static bool operator !=(EcmItem a, EcmItem b){
			return !(a == b);
		}

// インデクサ
		// 列名からデータを取得します。
		public string this[string colName]{
			get{ return GetData(colName); }
		}


// プロパティ

		// 完全修飾された ID を取得します。
		public override string FqId{
			get{return Project.Id + '/' + myId;}
		}

		// ファイルの最終アクセス日を示す文字列を取得します。ファイルがないときは "-" を返します。
		public string FileTime{
			get{
				if(this.File == null) return "-";
				if(!this.File.Exists) return "-";
				DateTime t = this.File.LastWriteTime;
				TimeSpan ts = Project.Setting.StartTime - t;
				if(ts < OneMinute) return string.Format("{0}s", ts.Seconds);
				if(ts < OneHour) return string.Format("{0}m", ts.Minutes);
				if(ts < OneDay) return string.Format("{0}h", ts.Hours);
				return  string.Format("{0}d", ts.Days);
			}
		}

		// この Item の元となった DataRow に直接アクセスします。
		public DataRow DataRow{
			get{return myRow;}
		}

		// この Item の DataRow のインデクスを取得します。
		public int RowIndex{
			get{return myRow[0].ToInt32();}
		}

		// パスを取得します。
		public override string Path{
			get{return myPath;}
		}

		// タイトルを取得します。
		public string Title{
			get{ return GetReservedData(ReservedTitleName);}
		}


		// この EcmItem に対応するプレビュー画像ファイルを示す FileInfo の配列を取得します。
		public FileInfo[] PreviewFiles{
			get{
				if(string.IsNullOrEmpty(Project.Setting.ImageDir)) return null;
				DirectoryInfo previewImageDir = Project.Setting.ImageFullPath;
				if(!previewImageDir.Exists) return null;
				FileInfo[] files = previewImageDir.GetFiles(this.Id + "*.*");
				Array.Sort(files, CompareFileInfoByName);
				return files;
			}
		}


		// この EcmItem に対応するグローバルテンプレートの名称を取得します。
		public string Template{
			get{
				string temp = GetReservedData(ReservedTemplateName);
				if(!string.IsNullOrEmpty(temp)) return temp;
				temp = Project.Setting.DefaultGrobalTemplate;
				if(!string.IsNullOrEmpty(temp)) return temp;
				return null;
			}
		}


		// この EcmItem に対応するスキーマの名称を取得します。
		public string SchemaName{
			get{
				string sch = GetReservedData(ReservedSchemaName);
				if(!string.IsNullOrEmpty(sch)) return sch;
				sch = Project.Setting.DefaultSchemaName;
				if(!string.IsNullOrEmpty(sch)) return sch;
				return null;
			}
		}

		// この EcmItem に対応するスキーマのファイルを取得します。
		public FileInfo SchemaFile{
			get{
				if(string.IsNullOrEmpty(SchemaName)) return null;
				DirectoryInfo schemaDir = Project.Setting.SchemaDirInfo;
				if(!schemaDir.Exists) return null;
				FileInfo[] files = schemaDir.GetFiles(SchemaName + ".xsd");
				if(files.Length > 0) return files[0];
				return null;
			}
		}


		// このプロジェクトに属するすべての EcmItem を取得します。
		public EcmItem[] AllItem{
			get{
				return Project.GetAllItems();
			}
		}


		// このプロジェクトに属する、自身を除いた EcmItem を取得します。
		public EcmItem[] AllOtherItem{
			get{
				List<EcmItem> otheritems = new List<EcmItem>();
				EcmItem[] items = Project.GetAllItems();
				for(int i = 0; i < items.Length; i++){
					if(items[i].FqId == this.FqId) continue;
					otheritems.Add(items[i]);
				}
				return otheritems.ToArray();
			}
		}


		// 親の EcmItem を取得します。
		public EcmItem ParentItem{
			get{
				if(string.IsNullOrEmpty(this.Parent)) return null;
				EcmItem result = Project.GetItem(this.Parent);
				if(result == null || result.FqId == this.FqId) return null;
				return result;
			}
		}


		// レベルを取得します。
		// 親が何人いるか取得します。トップはレベル0です。
		public int Level{
			get{
				int level = 0;
				EcmItem p = ParentItem;
				for(;;){
					if(p == null) return level;
					p = p.ParentItem;
					level++;
					if(level > Project.Setting.DepthMax) throw new Exception("レベルが取得できません。無限ループを検出しました。親の設定が循環している可能性があります。");
				}
			}
		}

		// 親のIDを取得します。
		public string Parent{
			get{return GetReservedData(ReservedParentName);}
		}

		// 子の EcmItem の配列を取得します。
		public EcmItem[] ChildItems{
			get{
				EcmItem[] result = Project.GetItemsByValue(ReservedParentName, Id);
				if(result != null && result.Length > 0) return result;
				result =  Project.GetItemsByValue(ReservedMark + ReservedParentName, Id);
				if(result != null) return result;
				return new EcmItem[0];
			}
		}

		// 兄弟の EcmItem の配列を取得します。自身も含まれます。
		// 親が無いときは空の配列が返ります。
		public EcmItem[] SiblingItems{
			get{
				if(this.Parent == null) return new EcmItem[0];
				return this.ParentItem.ChildItems;
			}
		}

		// 先祖の EcmItem の配列を取得します。自身は含まれません。
		// 親が無いときは空の配列が返ります。
		// 循環参照を検出した場合、そこで処理を打ち切ります。
		public EcmItem[] AncestorItems{
			get{
				if(this.Parent == null) return new EcmItem[0];
				// 循環参照チェック用
				List<EcmItem> al = new List<EcmItem>();

				EcmItem item = this.ParentItem;
				for(;;){
					if(item == null) break;
					// 循環参照チェック
					foreach(EcmItem i in al){
						if(i.FqId == item.FqId){
							goto AncestorRoopEnd;
						}
					}
					al.Add(item);
					item = item.ParentItem;
				}
				AncestorRoopEnd:
				al.Reverse();
				return al.ToArray();
			}
		}

		public bool IsAncestorOf(EcmItem target){
			if(target == null) return false;
			if(target.Parent == null) return false;

			// 循環参照チェック用
			List<EcmItem> al = new List<EcmItem>();

			EcmItem item = target.ParentItem;
			for(;;){
				if(item == null) return false;
				if(item == this) return true;
				// 循環参照チェック
				foreach(EcmItem i in al){
					if(i.FqId == item.FqId){
						return false;
					}
				}
				al.Add(item);
				item = item.ParentItem;
			}
		}

		// ParsePermissionRule に適合していれば true を返します。
		// ParsePermissionRule は 名前=値 の文字列です。
		// ParsePermissionRule が指定されていないとき、不正な値のときは true が返ります。
		public bool ParsePermit{
			get{
				if(Project.Setting.ParsePermissonRule == null) return true;
				string[] splitted = Project.Setting.ParsePermissonRule.Split(new char[]{'='});
				if(splitted.Length < 2) return true;
				// 判定する
				string colName = splitted[0].Trim();
				string colValue = splitted[1].Trim();
				if(this[colName] == null) return false;
				if(this[colName].Trim() == colValue) return true;
				return false;
			}
		}



// public メソッド

		// 任意の名前のデータを取得します。
		public string GetData(string colName){
			if(Project.Columns.IndexOf(colName) >= 0) return myRow[colName] as string;
			return null;
		}

		// 任意の名前の予約名データを取得します。
		public string GetReservedData(string colName){
			string result = GetData(ReservedMark + colName);
			if(result != null) return result;
			return GetData(colName);
		}

		// 列に指定された ID の EcmItem を取得します。
		public EcmItem GetEcmItem(string colName){
			string idName = GetData(colName);
			if(string.IsNullOrEmpty(idName)) return null;
			EcmItem result = Project.GetItem(idName);
			return result;
		}


		// リンクテキストとhref属性の値を指定してアンカーを取得します。
		public string GetAnchorByTextAndHref(string innerText, string href){
			string result = null;
			if(string.IsNullOrEmpty(href)){
				result = string.Format(Project.Setting.StayTemplate, innerText);
			} else {
				result = string.Format(Project.Setting.AnchorTemplate, href, innerText);
			}
			return result;
		}

		// リンクテキストを指定してアンカーを取得します。
		public string GetAnchorByText(string innerText){
			return GetAnchorByTextAndHref(innerText, this.RelUri());
		}

		// リンクテキストとなるパラメータの列名を指定してアンカーを取得します。
		public string GetAnchorByName(string colName){
			return GetAnchorByText(this[colName]);
		}

		// titleをリンクテキストとするアンカーを取得します。
		public string GetAnchor(){
			return GetAnchorByName(ReservedTitleName);
		}

		// リンクテキストを指定して絶対リンクアンカーを取得します。
		public string GetAbsUrlAnchorByText(string innerText){
			string href = string.Format("http://{0}{1}", Project.Setting.AbsUrlDomain, this.Path);
			return GetAnchorByTextAndHref(innerText, href);
		}

		// リンクテキストとなるパラメータの列名を指定して絶対URLアンカーを取得します。
		public string GetAbsUrlAnchorByName(string colName){
			return GetAbsUrlAnchorByText(this[colName]);
		}

		// 絶対URLにリンクするアンカーを取得します。
		public string GetAbsUrlAnchor(){
			return GetAbsUrlAnchorByName(ReservedTitleName);
		}




		// リンクテキストを指定して、ナビ用リンクのli要素を取得します。
		public string GetNavLinkByText(string innerText){
			string relUri = this.RelUri();
			string result = null;
			if(!this.Exists){
				result = string.Format(NavDisabledTemplate, innerText);
			} else if(string.IsNullOrEmpty(relUri)){
				result = string.Format(NavStayTemplate, innerText);
			} else {
				// 先祖リンク用テンプレートがある場合、リンク先が先祖か判定する
				if(!string.IsNullOrEmpty(AncestorNavLinkTemplate) && this.IsAncestorOf(Project.CurrentItem)){
					result = string.Format(AncestorNavLinkTemplate, relUri, innerText);
				} else {
					result = string.Format(NavLinkTemplate, relUri, innerText);
				}
			}
			return result;

		}

		// リンクテキストとなるパラメータの列名を指定して、ナビ用リンクのli要素を取得します。
		public string GetNavLinkByName(string colName){
			return GetNavLinkByText(this[colName]);
		}

		// titleをリンクテキストとするナビ用リンクのli要素を取得します。
		public string GetNavLink(){
			return GetNavLinkByName(ReservedTitleName);
		}

		// IDを返します。
		public override string ToString(){
			return Id;
		}

		// Format メソッド
		public new string Format(string s){
			string[] para = s.Split(',');
			object[] datas = new object[para.Length+1];
			datas[0] = RowIndex;
			for(int i=1; i< para.Length; i++){
				// 数値として解釈できるデータは数値とみなす
				string str = this[para[i]];
				int num = 0;
				if(Int32.TryParse(str, out num)){
					datas[i] = num;
				} else {
					datas[i] = str;
				}
			}
			return String.Format(para[0], datas);
		}

		// 与えられたパスに相当するファイルを取得します。
		public EcmTextFile GetFile(string path){
			string relPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), path);
			return new EcmTextFile(relPath, Project);
		}

		// 与えられたパスに相当する画像を取得します。
		public EcmImageFile GetImage(string path){
			string relPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), path);
			return new EcmImageFile(relPath, Project);
		}

		// 与えられたパスに相当するファイルを取得し、インクルードします。
		public string Include(string path){
			EcmTextFile file = GetFile(path);
			return file.ReadContent();
		}

		// 与えられたパスに相当する画像ファイルを取得し、img要素を生成します。
		public string ImgElement(string path){
			EcmImageFile img = GetImage(path);
			return img.ImgElement();
		}

	}
}

