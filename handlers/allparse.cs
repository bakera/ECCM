using System;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Xml;



namespace Bakera.Eccm{


	// ����v���W�F�N�g�̑S�f�[�^��publish����C���^�[�t�F�C�X��񋟂��܂��B
	public class AllParse : EcmProjectHandler{

		private static bool myParseFlag;
		private static int myCounter = 0; // publish�����J�E���^
		private static int myCounterMax = 0;


		public new const string PathName = "allparse";
		public new const string Name = "�ꊇpublish";

/*

�O��ꊇpublish����
�O��́u���̃v���W�F�N�g�́v�ꊇpublish���O��ۑ�

*/

// �R���X�g���N�^

		public AllParse(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}


// �v���p�e�B

		public override string SubTitle{
			get{return Name;}
		}

		// �ꊇpublish�̏�Ԃ�i�����m�F���܂��B
		public override EcmResponse Get(HttpRequest rq){
			XmlNode result = myXhtml.CreateDocumentFragment();

			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "�ꊇpublish�̏��";
			result.AppendChild(h3);

			if(myParseFlag){
				XmlElement p = myXhtml.Create("p", null, "���݈ꊇpublish�������ł��B���΂炭�҂��ă����[�h���Ă��������B");
				result.AppendChild(p);
				XmlElement p2 = myXhtml.Create("p", null, string.Format("�i��: {0}�� / �S{1}��", myCounter, myCounterMax));
				result.AppendChild(p2);
				return new HtmlResponse(myXhtml, result);
			}

			string resultLogName = GetResultLogName();
			if(File.Exists(resultLogName)){
				XmlElement descP = myXhtml.Create("p", null, string.Format("�O��publish���������� {0} �ł��B", File.GetLastWriteTime(resultLogName)));
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
				
				XmlElement h32 = myXhtml.H(3);
				h32.InnerText = "�O��publish���̃��O" ;
				result.AppendChild(h32);
				result.AppendChild(GetLogMessage(resultLogName));
			} else {
				XmlElement descP = myXhtml.Create("p", null, "�ꊇpublish�͍s���Ă��܂���B");
				result.AppendChild(descP);
				result.AppendChild(GetParseButton());
			}

			return new HtmlResponse(myXhtml, result);
		}

		private XmlElement GetParseButton(){
			XmlElement form = myXhtml.Form(null, "post");

			XmlElement descP = myXhtml.P();
			descP.InnerText = "���̃v���W�F�N�g�̈ꊇpublish���J�n����ɂ́A�u�ꊇpublish�J�n�v�{�^���������Ă��������B" ;
			form.AppendChild(descP);

			XmlElement formP = myXhtml.P();
			XmlElement parseSubmit = myXhtml.CreateSubmit("�ꊇpublish�J�n");
			formP.AppendChild(parseSubmit);
			form.AppendChild(formP);
			return form;
		}

		private XmlNode GetLogMessage(string filename){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			string logMessage = "";
			string infoStr = string.Format("[{0}]", EcmErrorLevel.Information);

			using(FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, Util.SjisEncoding)){
					while (sr.Peek() >= 0) {
						string temp = sr.ReadLine();
						if(string.IsNullOrEmpty(temp)) continue;
						if(temp.StartsWith(infoStr)) continue;
						logMessage += temp + "\n";
					}
					sr.Close();
				}
				fs.Close();
			}

			XmlElement pre = myXhtml.Create("pre");
			pre.InnerText = logMessage;
			result.AppendChild(pre);

			return result;
		}



		// �ꊇpublish�����s���܂��B
		public override EcmResponse Post(HttpRequest rq){
			XmlNode result = myXhtml.CreateDocumentFragment();

			if(myParseFlag){
				XmlElement h3 = myXhtml.H(3);
				h3.InnerText = "publish�ł��܂���";
				result.AppendChild(h3);

				XmlElement p = myXhtml.P();
				p.InnerText = "���݈ꊇpublish�������̂��߁A�ꊇpublish�����s���邱�Ƃ͂ł��܂���B";
				p.InnerText += string.Format("�i��: {0}�� / �S{1}��", myCounter, myCounterMax);
				result.AppendChild(p);
			} else {
				myParseFlag = true;
				Thread t = new Thread(new ThreadStart(ExecuteAllParse));
				t.Start();

				// ���������҂��Ă݂�

				for(int i=0; i < 5; i++){
					if(!t.IsAlive) return new HtmlResponse(myXhtml, EndMessage());
					Thread.Sleep(500);
				}

				// �I���Ȃ�����
				XmlElement h3 = myXhtml.H(3);
				h3.InnerText = "�ꊇpublish�J�n";
				result.AppendChild(h3);

				XmlElement p = myXhtml.P();
				p.InnerText = "�ꊇpublish�������J�n���܂������A�����Ɏ��Ԃ��������Ă��܂��B�����̓o�b�N�O���E���h�Ői�s���ł��B�i���⌋�ʂ̊m�F������ꍇ�́A";

				XmlElement a = myXhtml.Create("a");
				a.SetAttribute("href", PathName);
				a.InnerText = "�ꊇpublish�̏��";
				p.AppendChild(a);

				XmlText tx = myXhtml.Text("��ʂɖ߂�A�����[�h���Ă݂Ă��������B");
				p.AppendChild(tx);

				result.AppendChild(p);
			}

			return new HtmlResponse(myXhtml, result);
		}


// �v���C�x�[�g���\�b�h

		private XmlNode EndMessage(){
			XmlNode result = myXhtml.CreateDocumentFragment();
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "�ꊇpublish����";
			result.AppendChild(h3);

			XmlElement p = myXhtml.P();
			p.InnerText = "�ꊇpublish�������������܂����B";
			result.AppendChild(p);

			XmlElement h32 = myXhtml.H(3);
			h32.InnerText = "publish���̃��O" ;
			result.AppendChild(h32);

			result.AppendChild(GetLogMessage(GetResultLogName()));
			
			return result;
		}


		// �ꊇpublish�����s���܂��B
		private void ExecuteAllParse(){
			myParseFlag = true;
			string logFile = GetTempLogName();
			string resultFile = GetResultLogName();

			try{
				EcmItem[] items = myProject.GetAllItems();
				myCounter = 0;
				myCounterMax = items.Length;
				foreach(EcmItem i in  items){
					if(i.ParsePermit == false){
						string mes = string.Format("{0} : {1} ���X�L�b�v���܂� (publish������{2}�𖞂����܂���)�B\n\n", DateTime.Now, i, myProject.Setting.ParsePermissonRule);
						Util.AppendWriteFile(logFile, mes);
						continue;
					}

					// �p�[�T��p��
					Parser p = new Parser(myProject);
					// information �͋L�^���Ȃ�
					p.Log.MinimumErrorLevel = EcmErrorLevel.Important;

					ProcessResult pr = p.Process(i);
					Interlocked.Increment(ref myCounter);

					string parseStartMes = string.Format("{0} : {1} publish�J�n\n", DateTime.Now, i);
					Util.AppendWriteFile(logFile, parseStartMes);
					Util.AppendWriteFile(logFile, p.Log.ToString());
					if(p.Log.ErrorLevel < EcmErrorLevel.Error){
						string parseEndMes = string.Format("{0} : {1} publish����\n", DateTime.Now, i);
						Util.AppendWriteFile(logFile, parseEndMes);
					}
					Util.AppendWriteFile(logFile, string.Format("{0} : {1}\n", i, pr.Message));
				}
				Util.AppendWriteFile(logFile, myProject.Id + " : AllParse end");
				File.Copy(logFile, resultFile, true);
				File.Delete(logFile);
			} catch(Exception e) {
				Util.AppendWriteFile(logFile, e.ToString());
				File.Copy(logFile, resultFile, true);
				File.Delete(logFile);
			} finally {
				myParseFlag = false;
			}
		}


		// �ꊇpublish���ɐi���������o�����߂̃e���|�������O�t�@�C�������擾���܂��B
		// ���̃t�@�C���͑S�v���W�F�N�g�ŋ��ʂł��B
		private string GetTempLogName(){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("�v���W�F�N�g�f�B���N�g�������݂��܂��� : " + logDir);
			return logDir.TrimEnd('\\') + "\\allparse.temp";
		}

		// �ŏI�I�ɏ����o�����O�t�@�C�������擾���܂��B
		// ���̃f�B���N�g���͊e�v���W�F�N�g���ƂɈقȂ�܂��B
		private string GetResultLogName(){
			string logDir = myProject.Setting.BaseDir.FullName;
			if(!Directory.Exists(logDir)) throw new Exception("�v���W�F�N�g�f�B���N�g�������݂��܂��� : " + logDir);
			return logDir.TrimEnd('\\') + "\\allparse.log";
		}




	}
}


