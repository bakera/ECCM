using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Bakera.Eccm{
	public abstract class EcmProcessorBase{

		private Setting mySetting;
		private EcmProject myProject;
		private EcmLog myLog = new EcmLog();

		public const string Name = null;
		public const string Description = null;


// �R���X�g���N�^
		public EcmProcessorBase(EcmProject proj){
			myProject = proj;
			mySetting = proj.Setting;
		}


// �v���p�e�B

		public EcmProject Project{
			get{return myProject;}
		}

		public Setting Setting{
			get{return mySetting;}
		}

		public EcmLog Log{
			get{return myLog;}
		}


// public ���\�b�h
		// �^����ꂽ EcmItem �ɑΉ�����t�@�C���� Parse ���Ēu�����܂��B
		public abstract ProcessResult Process(EcmItem targetItem);


	}
}




