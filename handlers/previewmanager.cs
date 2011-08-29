using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{


	// �v���r���[�摜�̈ꗗ��\�����܂��B
	public class PreviewManager : EcmList{

		public PreviewManager(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "�v���r���[�摜";
		public new const string PathName = "preview";


// �v���p�e�B
		public override string SubTitle{
			get{return Name;}
		}


// ���\�b�h

		// �������Ȃ��Ƃ� : ����v���W�F�N�g�̃v���r���[�摜�ꗗ��\�����܂��B
		// ID ���n���ꂽ�Ƃ� : ���� ID �̃v���r���[�摜��Ԃ��܂��B
		public override EcmResponse Get(HttpRequest rq){
			if(!File.Exists(Setting.CsvFullPath)) return ProjectNull(Project.Id);
			if(myProject.DataCount == 0) return ShowError("�f�[�^������܂���BCSV �t�@�C���̓��e���m�F���Ă��������B");

			string[] imgIds = GetOptions(rq);
			if(imgIds.Length == 2){
				int imgNumber = 0;
				if(Int32.TryParse(imgIds[1], out imgNumber) && imgNumber > 0) return GetImage(imgIds[0], imgNumber);
			}

			// �摜�ꗗ
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			EcmItem[] items = myProject.GetAllItems();

			int itemCount = 0;
			XmlElement ul = myXhtml.Create("ul");
			foreach(EcmItem item in items){
				if(item.PreviewFiles != null && item.PreviewFiles.Length > 0){
					XmlElement li = myXhtml.Create("li");
					li.InnerText = item.Id;
					XmlElement innerUl = myXhtml.Create("ul");
					for(int i=0; i < item.PreviewFiles.Length; i++){
						string previewHref = PreviewManager.PathName + "/" + item.Id + "/" + (i+1).ToString();
						XmlElement previewA = myXhtml.Create("a");
						previewA.SetAttribute("href", previewHref);
						previewA.InnerText = item.PreviewFiles[i].Name;
						XmlElement previewLi = myXhtml.Create("li");
						previewLi.AppendChild(previewA);
						innerUl.AppendChild(previewLi);
						itemCount++;
					}
					li.AppendChild(innerUl);
					ul.AppendChild(li);
				}
			}
			if(itemCount > 0){
				result.AppendChild(ul);
				return new HtmlResponse(myXhtml, result);
			}
			return ShowError("�摜�͓o�^����Ă��܂���B");
		}

		private EcmResponse GetImage(string id, int num){
			EcmItem targetItem = myProject.GetItem(id);
			if(targetItem.PreviewFiles != null && targetItem.PreviewFiles.Length >= num){
				return new ImageResponse(targetItem.PreviewFiles[num-1]);
			}
			return ShowError("id: {0} �̉摜 {1} �݂͂���܂���ł����B", id, num.ToString());
		}


		// POST �ɂ͑Ή����܂��� (GET �Ɠ���)�B
		public override EcmResponse Post(HttpRequest rq){
			return Get(rq);
		}


		private XmlNode GetTable(EcmItem[] items){
			return null;
		}

		protected string[] GetOptions(HttpRequest rq){
			string thisPath = "/" + myProject.Id + "/" + PathName + "/";
			string optionId = Util.CutLeft(rq.PathInfo, thisPath);
			string[] optionIds = optionId.Split('/');
			return optionIds;
		}


	}
}


