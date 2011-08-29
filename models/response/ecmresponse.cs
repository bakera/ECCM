using System;
using System.Web;
using System.Xml;

namespace Bakera.Eccm{

	public abstract class EcmResponse{

		private int myStatus = 200;
		private HttpCookieCollection myCookies = new HttpCookieCollection();

		public int Status{
			get{return myStatus;}
			set{myStatus = value;}
		}

		public HttpCookieCollection Cookies{
			get{return myCookies;}
			set{myCookies = value;}
		}

		public abstract string ContentType{
			get;
		}

		public abstract void WriteResponse(HttpResponse response);

		
	}

}

