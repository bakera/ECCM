using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Serialization;


namespace Bakera.Eccm{


	// �v���O�C���̏�Ԃ�\�����܂��B
	public class PluginManager : EcmList{

		public PluginManager(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "�v���O�C��";
		public new const string PathName = "plugins";
		private FileInfo myTargetSrc;
		private static Compiler myCompiler = new Compiler();


// �v���p�e�B
		public override string SubTitle{
			get{return Name;}
		}


// Get/Post ���\�b�h

		// �������Ȃ��Ƃ� : ����v���W�F�N�g�̃v���r���[�摜�ꗗ��\�����܂��B
		// ID ���n���ꂽ�Ƃ� : ���� ID �̃v���r���[�摜��Ԃ��܂��B
		public override EcmResponse Get(HttpRequest rq){
			string testSrcId = GetSrcId(rq);

			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			if(string.IsNullOrEmpty(testSrcId)){
				result.AppendChild(ViewList());
			} else {
				result.AppendChild(ViewSrc(testSrcId));
			}
			return new HtmlResponse(myXhtml, result);
		}


		// POST 
		public override EcmResponse Post(HttpRequest rq){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string testSrcId = GetSrcId(rq);
			if(string.IsNullOrEmpty(testSrcId)){
				result.AppendChild(CompileAll());
			} else {
				result.AppendChild(CompileTest(testSrcId));
			}
			return new HtmlResponse(myXhtml, result);
		}


// private ���\�b�h

// �ꗗ�\��
		// �ꗗ��\�����܂� (�f�t�H���g)
		private XmlNode ViewList(){

			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			if(myProject.Setting.PluginKind == PluginKind.None){
				result.AppendChild(myXhtml.P(null, "�v���O�C���̌��ꂪ�w�肳��Ă��Ȃ����߁A�v���O�C�����g�p�ł��܂���B�v���O�C�����g�p����ɂ́A�u�ݒ�v����u�v���O�C���̌���v�̐ݒ���s���Ă��������B"));
			} else {
				result.AppendChild(ViewPluginType());
				result.AppendChild(ViewPlugins());
				result.AppendChild(ViewFiles());
				result.AppendChild(ViewCompilerState());
				result.AppendChild(ViewTestFiles());
			}
			return result;
		}

		// �R���p�C���ς݃v���O�C���̏���\�����܂��B
		private XmlNode ViewPluginType(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			result.AppendChild(myXhtml.P(null, "�v���O�C���̌��� : ", myProject.Setting.PluginKind));
			return result;
		}

		// �R���p�C���ς݃v���O�C���̏���\�����܂��B
		private XmlNode ViewPlugins(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			Type[] typeList = myProject.GetPlugins();
			XmlElement h2 = myXhtml.H(2, null, "�R���p�C���ς݃v���O�C��");
			result.AppendChild(h2);

			if(myProject.BinDirectory == null){
				result.AppendChild(myXhtml.P("note", "�v���O�C�� DLL �t�@�C�����̂̃p�X��H��܂���Bweb.config �� bin�f�B���N�g���̏�񂪐������ݒ肳��Ă��邩�m�F���Ă��������B"));
			} else if(myProject.PluginAssembly == null){
				result.AppendChild(myXhtml.P("note", "�v���O�C���͂���܂���B"));
			} else {
				FileInfo f = myProject.GetPluginAsmFile();
				AssemblyName an = myProject.PluginAssembly.GetName();
				result.AppendChild(myXhtml.P(null, string.Format("{0} : {1}", f.Name, f.LastWriteTime)));
			}

			if(typeList.Length > 0){
				XmlElement ul = myXhtml.Create("ul");
				foreach(Type t in typeList){
					XmlElement li = myXhtml.Create("li");
					li.InnerText = t.Name;
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
			} else {
				XmlElement p = myXhtml.P("note", "�v���O�C���͂���܂���B");
				result.AppendChild(p);
			}
			return result;
		}

		// �R���p�C���ς݃v���O�C���̏���\�����܂��B
		private XmlNode ViewCompilerState(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			XmlElement h2 = myXhtml.H(2, null, "�R���p�C���̎Q�ƃA�Z���u��");
			result.AppendChild(h2);
			foreach(string s in myCompiler.CurrentReferenceAsmNames){
				result.AppendChild(myXhtml.P(null, s));
			}

			return result;
		}


		// �\�[�X�t�@�C���̈ꗗ��\�����܂��B
		private XmlNode ViewFiles(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			XmlElement h2 = myXhtml.H(2, null, "�v���O�C���\�[�X�t�@�C���ꗗ");
			result.AppendChild(h2);

			FileInfo[] files = myProject.GetSrcFiles("*");
			if(files.Length == 0){
				result.AppendChild(myXhtml.P("note", "�v���O�C���\�[�X�t�@�C���͂���܂���B"));
			} else {
				XmlElement ul = myXhtml.Create("ul");
				foreach(FileInfo f in files){
					XmlElement li = myXhtml.Create("li");
					li.AppendChild(ViewFileInfo(f));
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
				result.AppendChild(CompileButton());
			}
			return result;
		}

		// �R���p�C���{�^����\�����܂��B
		private XmlNode CompileButton(){
			XmlElement form = myXhtml.Form(null, "post");
			XmlElement submitP = myXhtml.P("submit", myXhtml.CreateSubmit("�R���p�C��"));
			form.AppendChild(submitP);
			return form;
		}

		// �e�X�g�t�@�C���̈ꗗ��\�����܂��B
		private XmlNode ViewTestFiles(){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			XmlElement h2 = myXhtml.H(2, null, "�v���O�C���e�X�g�t�@�C���ꗗ");
			result.AppendChild(h2);

			FileInfo[] files = myProject.GetTestFiles("*");
			if(files.Length == 0){
				result.AppendChild(myXhtml.P("note", "�v���O�C���e�X�g�t�@�C���͂���܂���B"));
			} else {
				XmlElement ul = myXhtml.Create("ul");
				foreach(FileInfo f in files){
					XmlElement li = myXhtml.Create("li");
					li.AppendChild(ViewFileInfo(f));
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
			}
			return result;
		}


// �ڍו\��
		private XmlNode ViewSrc(string srcId){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			FileInfo[] files = myProject.GetFiles(srcId);
			if(files.Length == 0){
				string mes = string.Format("�v���O�C���t�@�C�� {0} ���݂���܂���B", srcId);
				return myXhtml.P(null, mes);
			}
			myTargetSrc = files[0];
			result.AppendChild(ViewFileInfo(myTargetSrc));

			string srcData = null;
			using(FileStream fs = myTargetSrc.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs)){
					srcData = sr.ReadToEnd();
					sr.Close();
				}
				fs.Close();
			}
			XmlElement pre = Html.Create("pre", "source");
			pre.InnerText = srcData;
			result.AppendChild(pre);

			XmlElement form = myXhtml.Form(null, "post");
			XmlElement submitP = myXhtml.P("submit", myXhtml.CreateSubmit("�e�X�g"));
			form.AppendChild(submitP);
			result.AppendChild(form);
			return result;
		}



		private XmlNode ViewFileInfo(FileInfo f){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string hrefStr = PluginManager.PathName + '/' + Util.CutRight(f.Name, myProject.PluginSourceSuffix);
			string info = string.Format(" {0} ({1}bytes)", f.LastWriteTime, f.Length);

			if(f == myTargetSrc){
				result.AppendChild(myXhtml.Text(f.Name));
			} else {
				XmlElement a = myXhtml.Create("a");
				a.InnerText = f.Name;
				a.SetAttribute("href", hrefStr);
				result.AppendChild(a);
			}
			result.AppendChild(myXhtml.Text(info));
			return result;
		}

// �R���p�C��

		// ���ׂẴ\�[�X�t�@�C�����R���p�C�����܂��B
		private XmlNode CompileAll(){
			FileInfo[] files = myProject.GetSrcFiles("*");
			if(files.Length == 0){
				string mes = "�v���O�C���\�[�X�t�@�C�����݂���܂���B";
				return myXhtml.P(null, mes);
			}

			FileInfo asmFile = myProject.GetPluginAsmFile();
			CompilerResults cr = myCompiler.Compile(files, asmFile, myProject);
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			if(cr.NativeCompilerReturnValue == 0){
				result.AppendChild(myXhtml.H(2, null, "�R���p�C������"));
				result.AppendChild(myXhtml.P(null, "�v���O�C���̃R���p�C���ɐ������܂����B"));
			} else {
				result.AppendChild(myXhtml.H(2, null, "�R���p�C���G���["));
				result.AppendChild(myXhtml.P(null, "�v���O�C���̃R���p�C���Ɏ��s���܂����B"));
			}
			if(cr.Errors.HasErrors){
				foreach(CompilerError ce in cr.Errors){
					result.AppendChild(myXhtml.P(null, ce));
				}
				return result;
			}
			if(cr.Output.Count > 0){
				XmlElement ol = myXhtml.Create("ol");
				foreach(string s in cr.Output){
					ol.AppendChild(myXhtml.Create("li", null, s));
				}
				result.AppendChild(ol);
			}
			return result;
		}


		// �R���p�C���̃e�X�g���s���܂��B
		private XmlNode CompileTest(string srcId){
			FileInfo[] files = myProject.GetFiles(srcId);
			if(files.Length == 0){
				string mes = string.Format("�v���O�C���t�@�C�� {0} ���݂���܂���B", srcId);
				return myXhtml.P(null, mes);
			}
			myTargetSrc = files[0];
			CompilerResults cr = myCompiler.Compile(myTargetSrc, null, myProject);
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			if(cr.NativeCompilerReturnValue == 0){
				result.AppendChild(myXhtml.H(2, null, "�R���p�C������"));
				result.AppendChild(myXhtml.P(null, srcId, " �̃R���p�C���ɐ������܂����B"));
//				Assembly compiledAsm = cr.CompiledAssembly;
			} else {
				result.AppendChild(myXhtml.H(2, null, "�R���p�C���G���["));
				result.AppendChild(myXhtml.P(null, srcId, " �̃R���p�C���Ɏ��s���܂����B"));
			}
			if(cr.Errors.HasErrors){
				foreach(CompilerError ce in cr.Errors){
					result.AppendChild(myXhtml.P(null, ce));
				}
				return result;
			}
			if(cr.Output.Count > 0){
				XmlElement ol = myXhtml.Create("ol");
				foreach(string s in cr.Output){
					ol.AppendChild(myXhtml.Create("li", null, s));
				}
				result.AppendChild(ol);
			}

			XmlElement form = myXhtml.Form(null, "post");
			XmlElement submitP = myXhtml.P("submit", myXhtml.CreateSubmit("�ăe�X�g"));
			form.AppendChild(submitP);
			result.AppendChild(form);

			return result;
		}


		private string GetSrcId(HttpRequest rq){
			string thisPath = "/" + myProject.Id + "/" + PathName;
			return Util.CutLeft(rq.PathInfo, thisPath).Trim('/');
		}

	}
}


