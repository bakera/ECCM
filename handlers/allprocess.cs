using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Serialization;


namespace Bakera.Eccm{


	// �v���W�F�N�g�S�̂���������ꊇ�����c�[���������܂��B
	public class AllProcess : EcmList{

		public AllProcess(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "�ꊇ����";
		public new const string PathName = "allprocess";

		private static Type myProcessorType;
		private static Thread myThread;
		private EventWaitHandle myProcessorHandler;
		private const int WaitTime = 5000;// �҂�����

		private static int myCounter = 0; // publish�����J�E���^
		private static int myCounterMax = 0;


// �v���p�e�B
		public override string SubTitle{
			get{return Name;}
		}

		public Type ProcessorType{
			get{return myProcessorType;}
		}


// Get/Post ���\�b�h

		public override EcmResponse Get(HttpRequest rq){
			XmlNode result = myXhtml.CreateDocumentFragment();

			if(myThread != null && myThread.IsAlive){
				if(ProcessorType != null){
					XmlElement p = myXhtml.Create("p", null, "����" + ProcessorType.Name + "�ɂ��ꊇ�������ł��B���΂炭�҂��ă����[�h���Ă��������B");
					result.AppendChild(p);
				}
				XmlElement p2 = myXhtml.Create("p", null, string.Format("�i��: {0}�� / �S{1}��", myCounter, myCounterMax));
				result.AppendChild(p2);
				result.AppendChild(AbortForm());
				return new HtmlResponse(myXhtml, result);
			}

			string[] options = GetOptions(rq);
			Type pType = null;
			if(options.Length > 0){
				pType = GetProcessorType(options[0]);
			}

			if(pType == null){
				XmlElement ul = myXhtml.Create("ul");
				foreach(Type t in Setting.ProcessorTypes){
					XmlElement li = myXhtml.Create("li");
					string hrefStr = PathName + "/" + t.Name;
					XmlElement a = myXhtml.Create("a");
					a.SetAttribute("href", hrefStr);
					a.InnerText = Util.GetFieldValue(t, "Name") + " / " + t.Name;
					li.AppendChild(myXhtml.P(null, a));
					li.AppendChild(myXhtml.P(null, Util.GetFieldValue(t, "Description")));
					ul.AppendChild(li);
				}
				result.AppendChild(ul);
				return new HtmlResponse(myXhtml, result);
			}

			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = pType.Name + "�ꊇ�����̏��";
			result.AppendChild(h3);

			string resultLogName = GetResultLogName(pType.Name);
			if(File.Exists(resultLogName)){
				XmlElement descP = myXhtml.Create("p", null, string.Format("�����͏I�����Ă��܂��B�O��̏����I�������� {0} �ł��B", File.GetLastWriteTime(resultLogName)));
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
				result.AppendChild(PreviousLog(pType));
			} else {
				XmlElement descP = myXhtml.Create("p", null, "�ꊇ�����͍s���Ă��܂���B");
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
			}

			return new HtmlResponse(myXhtml, result);
		}

		private XmlElement GetParseButton(){
			XmlElement form = myXhtml.Form(null, "post");

			XmlElement descP = myXhtml.P();
			descP.InnerText = "���̃v���W�F�N�g�̈ꊇ�������J�n����ɂ́A�u�ꊇ�����J�n�v�{�^���������Ă��������B" ;
			form.AppendChild(descP);

			XmlElement formP = myXhtml.P();
			XmlElement parseSubmit = myXhtml.CreateSubmit("�ꊇ�����J�n");
			formP.AppendChild(parseSubmit);
			form.AppendChild(formP);
			return form;
		}



		// �ꊇ���������s/��~���܂��B
		public override EcmResponse Post(HttpRequest rq){
			string[] options = GetOptions(rq);
			Type pType = null;
			if(options.Length > 0){
				pType = GetProcessorType(options[0]);
			}

			// ���~
			string action = rq["action"];
			if(!string.IsNullOrEmpty(action)){
				if(action.Equals("abort", StringComparison.CurrentCultureIgnoreCase)){
					if(myThread == null || !myThread.IsAlive) return Get(rq);
					myThread.Abort();
					return new HtmlResponse(myXhtml, ThreadAbortMessage());
				}
			}

			if(pType == null) return new HtmlResponse(myXhtml, NoHandlerErrorMessage());
			if(myThread != null && myThread.IsAlive) return new HtmlResponse(myXhtml, ProcessingErrorMessage());

			// �J�n
			myProcessorHandler = new EventWaitHandle(false, EventResetMode.AutoReset);
			myProcessorType = pType;
			myThread = new Thread(new ThreadStart(ExecuteAllProcess));
			myThread.Start();
			
			// ������Ƒ҂�
			bool waitResult = WaitHandle.WaitAll(new WaitHandle[]{myProcessorHandler}, WaitTime, false);
			if(waitResult) return new HtmlResponse(myXhtml, EndMessage());

			// �I���Ȃ�����
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "�ꊇ�����J�n";
			result.AppendChild(h3);

			XmlElement p = myXhtml.P();
			p.InnerText = pType.Name + "�ɂ��ꊇ�������J�n���܂������A�����Ɏ��Ԃ��������Ă��܂��B�����̓o�b�N�O���E���h�Ői�s���ł��B�i���⌋�ʂ̊m�F������ꍇ�́A";

			XmlElement a = myXhtml.Create("a");
			a.SetAttribute("href", ProcessorType.Name);
			a.InnerText = ProcessorType.Name + "�ꊇ�����̏��";
			p.AppendChild(a);

			XmlText tx = myXhtml.Text("��ʂɖ߂�A�����[�h���Ă݂Ă��������B");
			p.AppendChild(tx);

			XmlElement p2 = myXhtml.Create("p", null, string.Format("�i��: {0}�� / �S{1}��", myCounter, myCounterMax));
			result.AppendChild(p2);
			result.AppendChild(p);
			result.AppendChild(AbortForm());

			return new HtmlResponse(myXhtml, result);
		}



// �v���C�x�[�g���\�b�h

		private XmlNode EndMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = ProcessorType.Name + "�ꊇ��������";
			result.AppendChild(h3);

			XmlElement p = myXhtml.P();
			p.InnerText = "�ꊇ�������������܂����B";
			result.AppendChild(p);
			result.AppendChild(PreviousLog(ProcessorType));

			return result;
		}

		private XmlNode PreviousLog(Type t){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "���O";
			result.AppendChild(h3);
			result.AppendChild(GetLogMessage(GetResultLogName(t.Name)));
			return result;
		}


		private XmlNode NoHandlerErrorMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3unkown = myXhtml.H(3);
			h3unkown.InnerText = "�����ł��܂���";
			result.AppendChild(h3unkown);
			XmlElement punknown = myXhtml.P();
			punknown.InnerText = "�s���ȃn���h���ł��B���������s���邱�Ƃ͂ł��܂���B";
			result.AppendChild(punknown);
			return result;
		}

		private XmlNode ProcessingErrorMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "�����ł��܂���";
			result.AppendChild(h3);
			XmlElement p = myXhtml.P();
			if(myProcessorType != null){
				p.InnerText = "����" + myProcessorType.Name + "�̈ꊇ�������̂��߁A�V���ȏ��������s���邱�Ƃ͂ł��܂���B�������I���܂ő҂��Ă��������B";
			}
			p.InnerText += string.Format("�i��: {0}�� / �S{1}��", myCounter, myCounterMax);
			result.AppendChild(p);
			result.AppendChild(AbortForm());
			return result;
		}

		private XmlNode ThreadAbortMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "�ꊇ�����̋����I��";
			result.AppendChild(h3);
			XmlElement p = myXhtml.P();
			if(myProcessorType != null){
				p.InnerText = ProcessorType.Name + "�̈ꊇ�����������I�����܂����B";
			}
			result.AppendChild(p);
			result.AppendChild(PreviousLog(ProcessorType));
			return result;
		}


		private XmlNode AbortForm(){
			XmlElement form = myXhtml.Form(null, "post");
			XmlElement abortSubmit = myXhtml.CreateSubmit("�ꊇ�����������I������");
			XmlNode abortHidden = myXhtml.Hidden("action", "abort");
			XmlElement formP = myXhtml.P(null,  abortHidden, abortSubmit);
			form.AppendChild(formP);
			return form;
		}


		protected string[] GetOptions(HttpRequest rq){
			string thisPath = "/" + myProject.Id + "/" + PathName + "/";
			string optionId = Util.CutLeft(rq.PathInfo, thisPath);
			string[] optionIds = optionId.Split('/');
			return optionIds;
		}


		private XmlNode GetLogMessage(string filename){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string infoStr = string.Format("[{0}]", EcmErrorLevel.Information);
			XmlElement ol = myXhtml.Create("ol");

			using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, Util.SjisEncoding)){
					while (sr.Peek() >= 0) {
						string temp = sr.ReadLine();
						XmlElement li = myXhtml.Create("li");
						if(temp.StartsWith(infoStr)) li.SetAttribute("class", "info");
						li.InnerText = temp;
						ol.AppendChild(li);
					}
					sr.Close();
				}
				fs.Close();
			}
			result.AppendChild(ol);
			return result;
		}


		// �ꊇ���������s���܂��B
		private void ExecuteAllProcess(){
			string logFile = GetTempLogName(myProcessorType.Name);
			string resultFile = GetResultLogName(myProcessorType.Name);

			try{
				EcmItem[] items = myProject.GetAllItems();
				myCounter = 0;
				myCounterMax = items.Length;
				foreach(EcmItem i in items){
					if(i.ParsePermit == false){
						string mes = string.Format("{0} : {1} ���X�L�b�v���܂� (publish������{2}�𖞂����܂���)�B\n", DateTime.Now, i, myProject.Setting.ParsePermissonRule);
						Util.AppendWriteFile(logFile, mes);
						continue;
					}

					// �v���Z�b�T��p��
					ConstructorInfo ci = ProcessorType.GetConstructor(new Type[]{typeof(EcmProject)});
					if(ci == null) throw new Exception(ProcessorType.Name + "�ɂ́AEcmProject �������Ɏ��R���X�g���N�^������܂���B");
					Object o = ci.Invoke(new Object[]{myProject});
					EcmProcessorBase p = o as EcmProcessorBase;
					// information �͋L�^���Ȃ�
					p.Log.MinimumErrorLevel = EcmErrorLevel.Important;


					string parseStartMes = string.Format("{0} : {1} {2}�����J�n\n", DateTime.Now, i, ProcessorType.Name);
					ProcessResult pr = p.Process(i);
					Interlocked.Increment(ref myCounter);
					Util.AppendWriteFile(logFile, parseStartMes);
					Util.AppendWriteFile(logFile, p.Log.ToString());
					if(p.Log.ErrorLevel < EcmErrorLevel.Error){
						string parseEndMes = string.Format("{0} : {1} �����I��\n", DateTime.Now, i);
						Util.AppendWriteFile(logFile, parseEndMes);
					}
					if(!String.IsNullOrEmpty(pr.Message)){
						Util.AppendWriteFile(logFile, string.Format("{0} : {1}\n", i, pr.Message));
						Util.AppendWriteFile(logFile, "\n");
					}
					foreach(string errroMes in pr.Errors){
						Util.AppendWriteFile(logFile, string.Format("{0} : [Error] {1}\n", i, errroMes));
						Util.AppendWriteFile(logFile, "\n");
					}
				}
				Util.AppendWriteFile(logFile, myProject.Id + " : �ꊇ�������������܂����B");
			} catch(ThreadAbortException) {
				Util.AppendWriteFile(logFile, "�����͋����I������܂����B");
			} catch(Exception e) {
				Util.AppendWriteFile(logFile, e.ToString());
			} finally {
				File.Copy(logFile, resultFile, true);
				File.Delete(logFile);
				myProcessorHandler.Set();
			}
		}


		// �ꊇpublish���ɐi���������o�����߂̃e���|�������O�t�@�C�������擾���܂��B
		// ���̃t�@�C���͑S�v���W�F�N�g�ŋ��ʂł��B
		private string GetTempLogName(string s){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("�v���W�F�N�g�f�B���N�g�������݂��܂��� : " + logDir);
			return logDir.TrimEnd('\\') + "\\" + s + "_allprocess.temp";
		}

		// �ŏI�I�ɏ����o�����O�t�@�C�������擾���܂��B
		// ���̃f�B���N�g���͊e�v���W�F�N�g���ƂɈقȂ�܂��B
		private string GetResultLogName(string s){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("�v���W�F�N�g�f�B���N�g�������݂��܂��� : " + logDir);
			return logDir.TrimEnd('\\') + "\\" + s + "_allprocess.log";
		}

		// �w�肳�ꂽ�̖��O�̃v���Z�b�T���擾���܂��B
		private Type GetProcessorType(string s){
			foreach(Type t in Setting.ProcessorTypes){
				if(s.Equals(t.Name, StringComparison.CurrentCultureIgnoreCase)) return t;
			}
			return null;
		}
	}
}


