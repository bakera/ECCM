using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{


	// ����v���W�F�N�g�̐ݒ��\�����܂��B
	public class TreeView : EcmProjectHandler{

		public TreeView(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "�c���[�\��";
		public new const string PathName = "treeview";


		private int depthCount = 0;

// �v���p�e�B
		public override string SubTitle{
			get{return Name;}
		}


// ���\�b�h
		// �c���[�r���[��\�����܂��B
		public override EcmResponse Get(HttpRequest rq){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			EcmItem[] allItem = myProject.GetAllItems();
			XmlElement ul = myXhtml.Create("ul");
			foreach(EcmItem item in allItem){
				if(item.ParentItem != null) continue;
				XmlElement li = myXhtml.Create("li");
				li.AppendChild(GetAnchor(item));
				li.AppendChild(GetChildItemUl(item));
				ul.AppendChild(li);
			}
			result.AppendChild(ul);
			return new HtmlResponse(myXhtml, result);
		}

		private XmlNode GetChildItemUl(EcmItem item){
			EcmItem[] children = item.ChildItems;
			if(children.Length <= 0) return myXhtml.CreateDocumentFragment();
			if(depthCount++ > Setting.DepthMax) {
				throw new Exception("�������̏���𒴂��܂����B�������[�v�̉\��������܂��B" + item.ToString());
			}

			XmlElement result = myXhtml.Create("ul");
			foreach(EcmItem i in children){
				XmlElement li = myXhtml.Create("li");
				li.AppendChild(GetAnchor(i));
				li.AppendChild(GetChildItemUl(i));
				result.AppendChild(li);
			}
			return result;
		}

		private XmlNode GetAnchor(EcmItem item){
			XmlNode result = myXhtml.CreateDocumentFragment();
			string title = item.Id + " : " + item.Title;
			
			if(myProject.Setting.PreviewRootUrl != null && item.File != null && item.File.Exists){
				string previewUrl = myProject.Setting.PreviewRootUrl.TrimEnd('/') + item.Path;
				XmlElement a = myXhtml.Create("a");
				a.SetAttribute("href", previewUrl);
				a.InnerText = title;
				result.AppendChild(a);
			} else {
				result.AppendChild(myXhtml.Text(title));
			}
			return result;
		}


	}
}


