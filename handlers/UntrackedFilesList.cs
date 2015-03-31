using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{


	// �g���b�N����Ă��Ȃ�HTML�̈ꗗ��\�����܂��B
	public class UntrackedFilesList : EcmProjectHandler{

		public UntrackedFilesList(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "�Ǘ�����Ă��Ȃ��t�@�C���ꗗ";
		public new const string PathName = "untracked";

		private List<FileInfo> fileList = new List<FileInfo>();


// �v���p�e�B
		public override string SubTitle{
			get{return Name;}
		}


// ���\�b�h
		public override EcmResponse Get(HttpRequest rq){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			DirectoryInfo dir = myProject.Setting.DocumentFullPath;

			if(dir == null){
				return ShowError("myProject.Setting.DocumentFullPath�̒l���擾�ł��܂���ł����B");
			}
			if(!dir.Exists){
				return ShowError("�f�B���N�g�����݂���܂���: {0}", dir.FullName);
			}

			List<string> files = GetFilesFromDir(dir);

			XmlElement p = myXhtml.Create("p");
			p.InnerText = string.Format("�S�t�@�C����: {0}", files.Count);
			result.AppendChild(p);

			EcmItem[] allItem = myProject.GetAllItems();
			foreach(EcmItem ei in allItem){
				if(ei.File != null) files.Remove(ei.File.FullName);
			}

			XmlElement p2 = myXhtml.Create("p");
			p2.InnerText = string.Format("�Ǘ��O�t�@�C����: {0}", files.Count);
			result.AppendChild(p2);

			XmlElement ul = myXhtml.Create("ul");
			foreach(string f in files){
				XmlElement li = myXhtml.Create("li");
				li.InnerText = f;
				ul.AppendChild(li);
			}
			result.AppendChild(ul);


			return new HtmlResponse(myXhtml, result);
		}

		private List<string> GetFilesFromDir(DirectoryInfo dir){
			List<string> result = new List<string>();

			FileInfo[] files = dir.GetFiles("*.html");
			foreach(FileInfo f in files){
				result.Add(f.FullName);
			}

			DirectoryInfo[] subdirs = dir.GetDirectories();
			foreach(DirectoryInfo subdir in subdirs){
				result.AddRange(GetFilesFromDir(subdir));
			}
			return result;
		}


	}
}


