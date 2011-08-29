using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Bakera.Eccm{

	public class TemplateList : EcmProjectHandler{

		public new const string PathName = "templates";
		public new const string Name = "テンプレート一覧";

// コンストラクタ

		public TemplateList(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

// プロパティ
		public override string SubTitle{
			get{return Name;}
		}

// オーバーライドメソッド

		public override EcmResponse Get(HttpRequest rq){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			FileInfo[] files = Setting.TemplateFullPath.GetFiles("*." + Setting.TemplateExt.TrimStart('.'));
			if(files.Length == 0){
				return ShowError("テンプレートファイルがありません。");
			}

			XmlElement ul = myXhtml.Create("ul");
			foreach(FileInfo f in files){
				XmlElement li = myXhtml.Create("li");
				li.InnerText = f.Name;
				ul.AppendChild(li);
			}
			result.AppendChild(ul);
			return new HtmlResponse(myXhtml, result);
		}

		public override EcmResponse Post(HttpRequest rq){
			return Get(rq);
		}


	}

}



