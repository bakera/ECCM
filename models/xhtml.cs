using System;
using System.IO;
using System.Xml;

namespace Bakera.Eccm{

	/// <summary>
	/// XML DOM を利用して出力用の XHTML を簡単に作るためのクラスです。
	/// 外部実体は読みに行きません。
	/// </summary>
	public sealed class Xhtml : XmlDocument{
		private Uri myBaseUri;
		private XmlElement myEntry;
		public const string NameSpace = "http://www.w3.org/1999/xhtml";

// コンストラクタ

		/// <summary>
		/// XHTML ドキュメントのインスタンスを作成します。
		/// </summary>
		public Xhtml() : base(){
			XmlResolver = null;
		}

		/// <summary>
		/// ファイルを指定して、XHTML ドキュメントのインスタンスを作成します。
		/// </summary>
		public Xhtml(string filename) : this(){
			LoadFile(filename);
		}

		/// <summary>
		/// 雛形の Xhtml を指定して、Xhtml の新しいインスタンスを作成します。
		/// </summary>
		public Xhtml(Xhtml html) : this(){
			if(html == null) throw new Exception("元となる XHTML が null です。");
			foreach(XmlNode x in html.ChildNodes){
				AppendChild(this.ImportNode(x, true));
			}
		}

// プロパティ

		/// <summary>
		/// XHTML ドキュメントの基準となる URL を設定・取得します。
		/// </summary>
		public Uri BaseUri{
			get {return myBaseUri;}
			set {myBaseUri = value;}
		}

		/// <summary>
		/// エントリーポイントを設定・取得します。
		/// </summary>
		public XmlElement Entry{
			get {return myEntry;}
			set {myEntry = value;}
		}


// 各要素にアクセスするプロパティ

		/// <summary>
		/// XHTML ドキュメントの html 要素にアクセスします。
		/// </summary>
		public XmlElement Html{
			get {
				XmlElement result = this.DocumentElement;
				if(result == null){
					result = this.CreateElement("html");
					this.AppendChild(result);
				}
				return result;
			}
		}

		/// <summary>
		/// XHTML ドキュメントの head 要素を表す XmlElement にアクセスします。
		/// </summary>
		public XmlElement Head{
			get {
				XmlElement result = this.DocumentElement["head"];
				if(result == null){
					result = this.CreateElement("head");
					this.Html.PrependChild(result);
				}
				return result;
			}
		}

		/// <summary>
		/// XHTML ドキュメントの body 要素を表す XmlElement にアクセスします。
		/// </summary>
		public XmlElement Body{
			get {
				XmlElement result = this.DocumentElement["body"];
				if(result == null){
					result = this.CreateElement("body");
					this.Html.AppendChild(result);
				}
				return result;
			}
		}

		/// <summary>
		/// XHTML ドキュメントの title 要素を表す XmlElement にアクセスします。
		/// </summary>
		public XmlElement Title{
			get {
				XmlElement result = this.Head["title"];
				if(result == null){
					result = this.CreateElement("title");
					this.Head.AppendChild(result);
				}
				return result;
			}
		}

		/// <summary>
		/// XHTML ドキュメントの最初の h1 要素を表す XmlElement にアクセスします。
		/// h1 要素が無い場合は null を返します。
		/// </summary>
		public XmlElement H1{
			get {
				XmlNodeList nodes = this.Body.GetElementsByTagName("h1");
				if(nodes.Count == 0) return null;
				return nodes[0] as XmlElement;
			}
		}



// パブリックメソッド


		/// <summary>
		/// 要素名を指定して XmlElement を作成します。
		/// </summary>
		public XmlElement Create(string name){
			return base.CreateElement(name, NameSpace);
		}
		/// <summary>
		/// 要素名とクラス名を指定して XmlElement を作成します。
		/// </summary>
		public XmlElement Create(string name, string className){
			XmlElement result = Create(name);
			if(className != null) result.SetAttribute("class", className);
			return result;
		}
		/// <summary>
		/// 要素名、クラス名、内容の Object を指定して XmlElement を作成します。
		/// </summary>
		public XmlElement Create(string name, string className, params Object[] innerObj){
			XmlElement result = Create(name, className);
			if(innerObj == null) return result;
			foreach(Object o in innerObj){
				if(o == null) continue;
				if(o is XmlNode){
					 result.AppendChild(o as XmlNode);
				} else {
					result.AppendChild(CreateTextNode(o.ToString()));
				}
			}
			return result;
		}


		/// <summary>
		/// ファイル名を指定して XML データを読み取ります。
		/// Load と異なり、ソースファイルは読み取り禁止になりません (上書き禁止になるだけです)。
		/// </summary>
		public void LoadFile(string filename){
			using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)){
				Load(fs);
				fs.Close();
			}
		}


// 個別要素作成メソッド : Class 名指定可能

		/// <summary>
		/// Hn要素を作成します。
		/// </summary>
		public XmlElement H(int level){
			return H(level, null, null);
		}
		public XmlElement H(int level, string className){
			return H(level, className, null);
		}
		public XmlElement H(int level, string className, string innerText){
			string lStr = level.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if(level < 1) throw new Exception("Hn の見出しレベルとして " + lStr +" が指定されました。");

			if(level > 6){
				if(string.IsNullOrEmpty(className)){
					className = "level" + lStr;
				} else {
					className += " level" + lStr;
				}
				lStr = "6";
			}

			return Create("h" + lStr, className, innerText);
		}

		/// <summary>
		/// p要素を作成します。
		/// </summary>
		public XmlElement P(){
			return Create("p", null, null);
		}
		public XmlElement P(string className){
			return Create("p", className, null);
		}
		public XmlElement P(string className, params Object[] innerObj){
			return Create("p", className, innerObj);
		}



// カスタム要素作成メソッド

		/// <summary>
		/// href の文字列を指定して、a要素を作成します。
		/// 絶対 Uri は相対 Uri に変換されます。urn: などはそのまま出力されます。
		/// </summary>
		public XmlElement A(string uriStr){
			if(myBaseUri == null) throw new Exception("ベース URI が指定されていないため、URL を解決できません。");

			Uri uri = new Uri(myBaseUri, uriStr);

			if(uri == myBaseUri) return Create("em", "stay");

			XmlElement result = this.Create("a");
			if(uri != null){
				uri = MakeRelative(uri);
				result.SetAttribute("href", uri.ToString());
			}
			return result;
		}

		/// <summary>
		/// ラベルを指定して、submitボタンを作成します。
		/// </summary>
		public XmlElement CreateSubmit(string label){
			XmlElement result = this.Create("input");
			result.SetAttribute("type", "submit");
			if(label != null) result.SetAttribute("value", label);
			return result;
		}

		/// <summary>
		/// ラベルを指定して、Checkboxを作成します。
		/// </summary>
		public XmlNode Checkbox(string name, string value, bool boxChecked, string labelStr){
			XmlNode result = CreateDocumentFragment();
			
			XmlElement input = Create("input", "checkbox");
			input.SetAttribute("type", "checkbox");
			input.SetAttribute("name", name);
			input.SetAttribute("id", name);
			input.SetAttribute("value", value);
			if(boxChecked) input.SetAttribute("checked", "checked");

			result.AppendChild(input);

			XmlElement label = Create("label", null, labelStr);
			label.SetAttribute("for", name);
			result.AppendChild(label);

			return result;
		}


		/// <summary>
		/// 名前と値を指定して input 要素を作成します。
		/// </summary>
		public XmlNode Input(string name, string value, string labelStr){

			XmlElement input = Create("input", "text");
			input.SetAttribute("name", name);
			input.SetAttribute("id", name);
			input.SetAttribute("value", value);
			if(string.IsNullOrEmpty(labelStr)) return input;

			XmlNode result = CreateDocumentFragment();

			XmlElement label = Create("label", null, labelStr);
			label.SetAttribute("for", name);
			result.AppendChild(label);
			result.AppendChild(CreateTextNode(" : "));
			result.AppendChild(input);

			return result;
		}


		/// <summary>
		/// 名前と値を指定して input type="hidden" を作成します。
		/// </summary>
		public XmlNode Hidden(string name, string value){

			XmlElement input = Create("input");
			input.SetAttribute("type", "hidden");
			input.SetAttribute("name", name);
			input.SetAttribute("value", value);
			return input;
		}


		/// <summary>
		/// 自身に submit する form要素を作成します。
		/// </summary>
		public XmlElement Form(){return Form(null);}
		/// <summary>
		/// action を指定して、form要素を作成します。
		/// </summary>
		public XmlElement Form(Uri action){
			return Form(action, null);
		}
		/// <summary>
		/// action と method を指定して、form要素を作成します。
		/// </summary>
		public XmlElement Form(Uri action, string method){
			XmlElement result = this.Create("form");
			action = MakeRelative(action);
			string actionStr;
			if(action == null){
				actionStr = "";
			} else {
				actionStr = action.ToString();
			}
			result.SetAttribute("action", actionStr);
			if(method != null) result.SetAttribute("method", method);
			return result;
		}


		/// <summary>
		/// 見出しの各要素のXmlNodeを指定して、thead要素を作成します。各 th には scope="col" がつきます。
		/// </summary>
		public XmlElement CreateThead(params Object[] innerObj){
			if(innerObj == null) return null;
			XmlElement result = this.Create("thead");
			XmlElement tr = this.Create("tr");
			foreach(Object o in innerObj){
				XmlElement th = this.Create("th");
				if(o == null) {
					th.AppendChild(CreateTextNode(" "));
				} else if(o is XmlNode){
					th.AppendChild(o as XmlNode);
					th.SetAttribute("scope", "col");
				} else {
					th.AppendChild(CreateTextNode(o.ToString()));
					th.SetAttribute("scope", "col");
				}
				tr.AppendChild(th);
			}
			result.AppendChild(tr);
			return result;
		}


		/// <summary>
		/// 見出しの各要素のXmlNodeを指定して、tr要素を作成します。
		/// </summary>
		public XmlElement CreateTr(params Object[] innerObj){
			if(innerObj == null) return null;
			XmlElement result = this.Create("tr");
			foreach(Object o in innerObj){
				XmlElement td = this.Create("td");
				if(o == null) {
					td.AppendChild(CreateTextNode(" "));
				} else if(o is XmlNode){
					td.AppendChild(o as XmlNode);
				} else {
					td.AppendChild(CreateTextNode(o.ToString()));
				}
				result.AppendChild(td);
			}
			return result;
		}


// カスタムノード作成メソッド

		/// <summary>
		/// 半角スペースを含む TextNode を作成します。
		/// </summary>
		public XmlText Space(){
			return this.CreateTextNode(" ");
		}

		/// <summary>
		/// 指定されたテキストを含む TextNode を作成します。
		/// </summary>
		public XmlText Text(string s){
			return this.CreateTextNode(s);
		}


// メタ情報追加メソッド

		/// <summary>
		/// href の値を指定して、<link rel=stylesheet> を追加します。
		/// </summary>
		public void AddStyleLink(string href){
			AddStyleLink(href, null);
		}
		/// <summary>
		/// href の値と title を指定して、<link rel=stylesheet> を追加します。
		/// </summary>
		public void AddStyleLink(string href, string title){
			XmlElement link = this.Create("link");
			link.SetAttribute("rel", "stylesheet");
			link.SetAttribute("type", "text/css");
			link.SetAttribute("href", href);
			if(!string.IsNullOrEmpty(title)) link.SetAttribute("title", title);
			this.Head.AppendChild(link);
		}

// 要素変換

		/// <summary>
		/// href 属性を持つ任意の XmlElement を a要素に変換します。
		/// </summary>
		public XmlElement GetA(XmlElement elem){
			string href = elem.GetAttribute("href");
			XmlElement result = A(href);
			result.InnerText = elem.InnerText;
			return result;
		}


// その他メソッド

		/// <summary>
		/// 設定されたベース Uri を元に相対 Uri を生成します。
		/// </summary>
		public Uri MakeRelative(Uri uri){
			if(myBaseUri == null) return uri;
			if(myBaseUri == uri) return new Uri("", UriKind.Relative);

			if(uri == null) return new Uri("", UriKind.Relative);
			if(!uri.IsAbsoluteUri) return uri;

			Uri result = myBaseUri.MakeRelativeUri(uri);
			if(string.IsNullOrEmpty(result.ToString())) return new Uri("./", UriKind.Relative);
			return result;
		}

	} // End class OutXhtml
}
