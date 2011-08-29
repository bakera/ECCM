using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;

namespace Bakera.Eccm{

	// 特定プロジェクトに対して何かを表示・操作するための抽象クラスです。
	public abstract class EcmProjectHandler{

		public const string PathName = null;
		public const string Name = null;

		public const string EccmTitleFormat = "ECCM : {0}";

		protected Xhtml myXhtml = null;
		protected EcmProject myProject = null;



// コンストラクタ
		protected EcmProjectHandler(EcmProject proj, Xhtml xhtml){
			myProject = proj;
			myXhtml = xhtml;
		}


// プロパティ
		public virtual string SubTitle{
			get{return Name;}
		}

		public EcmProject Project{
			get{return myProject;}
		}

		public Xhtml Html{
			get{return myXhtml;}
		}

		public Setting Setting{
			get{return myProject.Setting;}
		}



// メソッド

		public virtual void SetTitle(){
			XmlElement h1 = myXhtml.H(1);
			string pName = myProject.ProjectName;
			if(pName == null) pName = string.Format("{0}(名称未設定プロジェクト)", myProject.Setting.Id);
			h1.InnerText = pName;
			if(this.SubTitle != null) h1.InnerText += " " + this.SubTitle;
			myXhtml.Title.InnerText = string.Format(EccmTitleFormat, h1.InnerText);
			myXhtml.Body.AppendChild(h1);
		}

		public abstract EcmResponse Get(HttpRequest rq);

		public virtual EcmResponse Post(HttpRequest rq){return null;}

		protected HtmlResponse ShowError(string format, params string[] strs){
			return ShowError(String.Format(format, strs));
		}

		protected HtmlResponse ShowError(string s){
			XmlElement p = myXhtml.Create("p");
			p.InnerText = s;
			myProject.Log.AddError(s);
			return new ErrorResponse(myXhtml, p);
		}





	}
}


