using System;
using System.Web;

namespace Bakera.Eccm{
	public abstract class EcmToolBase{

// �t�B�[���h
		protected readonly EcmPluginParams myParam;
		public static Type[] PluginConstractorParams = new Type[]{typeof(EcmPluginParams)};

// �R���X�g���N�^
		// Setting �� EcmProject, EcmItem ���w�肵�āAEcmPlugin �N���X�̃C���X�^���X���쐬���܂��B
		protected EcmToolBase(EcmPluginParams param){
			myParam = param;
		}


// �v���p�e�B

		protected EcmItem Item{
			get{return Project.CurrentItem;}
		}

		protected Setting Setting{
			get{return myParam.Setting;}
		}

		protected EcmProject Project{
			get{return myParam.Project;}
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

