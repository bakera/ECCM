using System;
using System.IO;
using System.Xml;

namespace Bakera.Eccm{

	/// <summary>
	/// XML DOM �𗘗p���ďo�͗p�� XHTML ���ȒP�ɍ�邽�߂̃N���X�ł��B
	/// �O�����͓̂ǂ݂ɍs���܂���B
	/// </summary>
	public sealed class Xhtml : XmlDocument{
		private Uri myBaseUri;
		private XmlElement myEntry;
		public const string NameSpace = "http://www.w3.org/1999/xhtml";

// �R���X�g���N�^

		/// <summary>
		/// XHTML �h�L�������g�̃C���X�^���X���쐬���܂��B
		/// </summary>
		public Xhtml() : base(){
			XmlResolver = null;
		}

		/// <summary>
		/// �t�@�C�����w�肵�āAXHTML �h�L�������g�̃C���X�^���X���쐬���܂��B
		/// </summary>
		public Xhtml(string filename) : this(){
			LoadFile(filename);
		}

		/// <summary>
		/// ���`�� Xhtml ���w�肵�āAXhtml �̐V�����C���X�^���X���쐬���܂��B
		/// </summary>
		public Xhtml(Xhtml html) : this(){
			if(html == null) throw new Exception("���ƂȂ� XHTML �� null �ł��B");
			foreach(XmlNode x in html.ChildNodes){
				AppendChild(this.ImportNode(x, true));
			}
		}

// �v���p�e�B

		/// <summary>
		/// XHTML �h�L�������g�̊�ƂȂ� URL ��ݒ�E�擾���܂��B
		/// </summary>
		public Uri BaseUri{
			get {return myBaseUri;}
			set {myBaseUri = value;}
		}

		/// <summary>
		/// �G���g���[�|�C���g��ݒ�E�擾���܂��B
		/// </summary>
		public XmlElement Entry{
			get {return myEntry;}
			set {myEntry = value;}
		}


// �e�v�f�ɃA�N�Z�X����v���p�e�B

		/// <summary>
		/// XHTML �h�L�������g�� html �v�f�ɃA�N�Z�X���܂��B
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
		/// XHTML �h�L�������g�� head �v�f��\�� XmlElement �ɃA�N�Z�X���܂��B
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
		/// XHTML �h�L�������g�� body �v�f��\�� XmlElement �ɃA�N�Z�X���܂��B
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
		/// XHTML �h�L�������g�� title �v�f��\�� XmlElement �ɃA�N�Z�X���܂��B
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
		/// XHTML �h�L�������g�̍ŏ��� h1 �v�f��\�� XmlElement �ɃA�N�Z�X���܂��B
		/// h1 �v�f�������ꍇ�� null ��Ԃ��܂��B
		/// </summary>
		public XmlElement H1{
			get {
				XmlNodeList nodes = this.Body.GetElementsByTagName("h1");
				if(nodes.Count == 0) return null;
				return nodes[0] as XmlElement;
			}
		}



// �p�u���b�N���\�b�h


		/// <summary>
		/// �v�f�����w�肵�� XmlElement ���쐬���܂��B
		/// </summary>
		public XmlElement Create(string name){
			return base.CreateElement(name, NameSpace);
		}
		/// <summary>
		/// �v�f���ƃN���X�����w�肵�� XmlElement ���쐬���܂��B
		/// </summary>
		public XmlElement Create(string name, string className){
			XmlElement result = Create(name);
			if(className != null) result.SetAttribute("class", className);
			return result;
		}
		/// <summary>
		/// �v�f���A�N���X���A���e�� Object ���w�肵�� XmlElement ���쐬���܂��B
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
		/// �t�@�C�������w�肵�� XML �f�[�^��ǂݎ��܂��B
		/// Load �ƈقȂ�A�\�[�X�t�@�C���͓ǂݎ��֎~�ɂȂ�܂��� (�㏑���֎~�ɂȂ邾���ł�)�B
		/// </summary>
		public void LoadFile(string filename){
			using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)){
				Load(fs);
				fs.Close();
			}
		}


// �ʗv�f�쐬���\�b�h : Class ���w��\

		/// <summary>
		/// Hn�v�f���쐬���܂��B
		/// </summary>
		public XmlElement H(int level){
			return H(level, null, null);
		}
		public XmlElement H(int level, string className){
			return H(level, className, null);
		}
		public XmlElement H(int level, string className, string innerText){
			string lStr = level.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if(level < 1) throw new Exception("Hn �̌��o�����x���Ƃ��� " + lStr +" ���w�肳��܂����B");

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
		/// p�v�f���쐬���܂��B
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



// �J�X�^���v�f�쐬���\�b�h

		/// <summary>
		/// href �̕�������w�肵�āAa�v�f���쐬���܂��B
		/// ��� Uri �͑��� Uri �ɕϊ�����܂��Burn: �Ȃǂ͂��̂܂܏o�͂���܂��B
		/// </summary>
		public XmlElement A(string uriStr){
			if(myBaseUri == null) throw new Exception("�x�[�X URI ���w�肳��Ă��Ȃ����߁AURL �������ł��܂���B");

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
		/// ���x�����w�肵�āAsubmit�{�^�����쐬���܂��B
		/// </summary>
		public XmlElement CreateSubmit(string label){
			XmlElement result = this.Create("input");
			result.SetAttribute("type", "submit");
			if(label != null) result.SetAttribute("value", label);
			return result;
		}

		/// <summary>
		/// ���x�����w�肵�āACheckbox���쐬���܂��B
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
		/// ���O�ƒl���w�肵�� input �v�f���쐬���܂��B
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
		/// ���O�ƒl���w�肵�� input type="hidden" ���쐬���܂��B
		/// </summary>
		public XmlNode Hidden(string name, string value){

			XmlElement input = Create("input");
			input.SetAttribute("type", "hidden");
			input.SetAttribute("name", name);
			input.SetAttribute("value", value);
			return input;
		}


		/// <summary>
		/// ���g�� submit ���� form�v�f���쐬���܂��B
		/// </summary>
		public XmlElement Form(){return Form(null);}
		/// <summary>
		/// action ���w�肵�āAform�v�f���쐬���܂��B
		/// </summary>
		public XmlElement Form(Uri action){
			return Form(action, null);
		}
		/// <summary>
		/// action �� method ���w�肵�āAform�v�f���쐬���܂��B
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
		/// ���o���̊e�v�f��XmlNode���w�肵�āAthead�v�f���쐬���܂��B�e th �ɂ� scope="col" �����܂��B
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
		/// ���o���̊e�v�f��XmlNode���w�肵�āAtr�v�f���쐬���܂��B
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


// �J�X�^���m�[�h�쐬���\�b�h

		/// <summary>
		/// ���p�X�y�[�X���܂� TextNode ���쐬���܂��B
		/// </summary>
		public XmlText Space(){
			return this.CreateTextNode(" ");
		}

		/// <summary>
		/// �w�肳�ꂽ�e�L�X�g���܂� TextNode ���쐬���܂��B
		/// </summary>
		public XmlText Text(string s){
			return this.CreateTextNode(s);
		}


// ���^���ǉ����\�b�h

		/// <summary>
		/// href �̒l���w�肵�āA<link rel=stylesheet> ��ǉ����܂��B
		/// </summary>
		public void AddStyleLink(string href){
			AddStyleLink(href, null);
		}
		/// <summary>
		/// href �̒l�� title ���w�肵�āA<link rel=stylesheet> ��ǉ����܂��B
		/// </summary>
		public void AddStyleLink(string href, string title){
			XmlElement link = this.Create("link");
			link.SetAttribute("rel", "stylesheet");
			link.SetAttribute("type", "text/css");
			link.SetAttribute("href", href);
			if(!string.IsNullOrEmpty(title)) link.SetAttribute("title", title);
			this.Head.AppendChild(link);
		}

// �v�f�ϊ�

		/// <summary>
		/// href ���������C�ӂ� XmlElement �� a�v�f�ɕϊ����܂��B
		/// </summary>
		public XmlElement GetA(XmlElement elem){
			string href = elem.GetAttribute("href");
			XmlElement result = A(href);
			result.InnerText = elem.InnerText;
			return result;
		}


// ���̑����\�b�h

		/// <summary>
		/// �ݒ肳�ꂽ�x�[�X Uri �����ɑ��� Uri �𐶐����܂��B
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
