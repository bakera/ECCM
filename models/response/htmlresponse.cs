using System;
using System.Web;
using System.Web.Configuration;
using System.Xml;

namespace Bakera.Eccm{

	public class HtmlResponse : EcmResponse{

		private Xhtml myXhtml;
		private string myMessage;


// コンストラクタ
		public HtmlResponse(){
			string templateFile = WebConfigurationManager.AppSettings["EccmTemplate"];
			myXhtml = new Xhtml(templateFile);
		}
		public HtmlResponse(XmlNode x){
			if(x is Xhtml){
				myXhtml = x as Xhtml;
			} else {
				string templateFile = WebConfigurationManager.AppSettings["EccmTemplate"];
				myXhtml = new Xhtml(templateFile);
				myXhtml.Body.AppendChild(x);
			}
		}
		public HtmlResponse(Xhtml xhtml, XmlNode x){
			myXhtml = xhtml;
			myXhtml.Body.AppendChild(x);
		}


// プロパティ

		public override string ContentType{
			get{return "text/html";}
		}

		public Xhtml BodyXml{
			get{return myXhtml;}
			set{myXhtml = value;}
		}

		public string Message{
			get{return myMessage;}
			set{myMessage = value;}
		}

		public override void WriteResponse(HttpResponse response){
			response.ContentType = "text/html";
			response.Charset = "UTF-8";
			if(myXhtml != null) response.Write(myXhtml.OuterXml);
			if(myMessage != null){
				myXhtml.Body.AppendChild(myXhtml.CreateTextNode(myMessage));
				response.Write(myXhtml.OuterXml);
 			}
 			for(int i=0; i < this.Cookies.Count; i++){
				response.Cookies.Add(this.Cookies[i]);
			}
		}
	}

}

