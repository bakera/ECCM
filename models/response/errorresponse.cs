using System;
using System.Web;
using System.Web.Configuration;
using System.Xml;

namespace Bakera.Eccm{

	public class ErrorResponse : HtmlResponse{
		public ErrorResponse(Xhtml xhtml, XmlNode x) : base(xhtml, x){
		}
	}

}

