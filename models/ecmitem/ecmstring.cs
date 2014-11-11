using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Web;


namespace Bakera.Eccm{


	// EcmItemなどの基底クラス
	public class EcmString{

		protected string myId = null;
		protected EcmProject myProject = null;

// コンストラクタ

		// ID と EcmProject から EcmItem を作成します。
		public EcmString(string id, EcmProject proj) : this (proj){
			myId = id;
		}

		public EcmString(EcmProject proj){
			myProject = proj;
		}


// public プロパティ

		// 処理中のParserを取得します。
		public Parser Parser{
			get; set;
		}

		// 親の EcmProject を取得します。
		public EcmProject Project{
			get{ return myProject; }
		}

		// ID を取得します。
		public string Id{
			get{return myId;}
			set{myId = value;}
		}

		// 完全修飾された ID を取得します。
		public virtual string FqId{
			get{return myId;}
		}

		// 末尾からindex.htmlなどを取り除いた文字列を取得します。
		public virtual string WithoutIndex{
			get{
				return Util.CutRight(myId, Project.Setting.IndexLinkSuffix);
			}
		}


// public メソッド

		public override string ToString(){
			return myId;
		}

		public string ToUpper(){
			return myId.ToUpper();
		}

		public string ToLower(){
			return myId.ToLower();
		}

		public string HtmlEncode(){
			return HttpUtility.HtmlEncode(myId);
		}

		public string Format(string s){
			return String.Format(s, myId);
		}

		public string Replace(string s){
			string[] replaceParams = s.Split(',');
			if(replaceParams.Length == 0) return myId;
			if(replaceParams.Length == 1){
				return myId.Replace(replaceParams[0], "");
			}
			return myId.Replace(replaceParams[0], replaceParams[1]);
		}


		public string Truncate(string s){
			try{
				int len = Convert.ToInt32(s);
				return myId.Substring(0, len);
			} catch {
				return null;
			}
		}
		
		public string UrlEncode(){
			return UrlEncode(myId);
		}

		public string UrlEncode(string s){
			return System.Web.HttpUtility.UrlEncode(s);
		}

		// Parserが存在するとき、このテキストをParseします。
		public string Parse(){
			return Parse(myId);
		}

		// Parserが存在するとき、渡されたテキストをParseします。
		public string Parse(string data){
			return Parser.GeneralParse(data);
		}


		// Parserが存在するとき、テンプレートを適用します。
		public string ApplyTemplate(string templateName){
			string mark = string.Format("<!--={0}/-->", templateName);
			return Parse(mark);
		}




// 静的メソッド

		// 与えられた文字列をそのまま返します。
		public static string Str(string s){
			return s;
		}

		// FileInfo を名前でソートするための比較メソッドです。
		public static int CompareFileInfoByName(FileInfo x, FileInfo y){
			if(x == null){
				if(y == null) return 0;
				return -1;
			}
			if(y == null) return 1;
			return String.Compare(x.Name, y.Name);
		}



	}
}

