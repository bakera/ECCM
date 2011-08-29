using System;
using System.Web;

namespace Bakera.Eccm{
	public abstract class EcmPluginBase{

// �t�B�[���h
		protected readonly EcmPluginParams myParam;
		protected readonly EcmPluginDocument myDocument = new EcmPluginDocument();
		public static Type[] PluginConstractorParams = new Type[]{typeof(EcmPluginParams)};

// �R���X�g���N�^
		// Setting �� EcmProject, EcmItem ���w�肵�āAEcmPlugin �N���X�̃C���X�^���X���쐬���܂��B
		protected EcmPluginBase(EcmPluginParams param){
			myParam = param;
		}


// �v���p�e�B

		protected EcmItem Item{
			get{return Project.CurrentItem;}
		}

		protected Parser Parser{
			get{return myParam.Parser;}
		}

		protected Setting Setting{
			get{return myParam.Setting;}
		}

		protected EcmProject Project{
			get{return myParam.Project;}
		}

		public EcmPluginDocument Document{
			get{return myDocument;}
		}

		public MarkedData MarkedData{
			get{return myParam.MarkedData;}
		}

		public int CalledCount{
			get{return myParam.CalledCount;}
		}


// ���ۃ��\�b�h

		public abstract void Parse();

// �ÓI���\�b�h

		// ��������uHTML �G���R�[�h�v���܂��B
		public static string HtmlEncode(object s){
			return HttpUtility.HtmlEncode(s.ToString());
		}

		// ��������uHTML �G���R�[�h�v���܂��B
		public static string H(object s){
			return HtmlEncode(s);
		}

		// ��������uHTML �G���R�[�h�v������ň��p���Ŋ���܂��B
		public static string Q(object s){
			return QuoteAttribute(s);
		}

		// ��������uHTML �G���R�[�h�v������ň��p���Ŋ���܂��B
		public static string QuoteAttribute(object s){
			return '"' + HttpUtility.HtmlEncode(s.ToString()) + '"';
		}


	}
}

