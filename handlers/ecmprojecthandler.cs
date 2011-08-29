using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;

namespace Bakera.Eccm{

	// ����v���W�F�N�g�ɑ΂��ĉ�����\���E���삷�邽�߂̒��ۃN���X�ł��B
	public abstract class EcmProjectHandler{

		public const string PathName = null;
		public const string Name = null;

		public const string EccmTitleFormat = "ECCM : {0}";

		protected Xhtml myXhtml = null;
		protected EcmProject myProject = null;



// �R���X�g���N�^
		protected EcmProjectHandler(EcmProject proj, Xhtml xhtml){
			myProject = proj;
			myXhtml = xhtml;
		}


// �v���p�e�B
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



// ���\�b�h

		public virtual void SetTitle(){
			XmlElement h1 = myXhtml.H(1);
			string pName = myProject.ProjectName;
			if(pName == null) pName = string.Format("{0}(���̖��ݒ�v���W�F�N�g)", myProject.Setting.Id);
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


