using System;
using System.Threading;

namespace Bakera.Eccm{
	public class TestProcessor : EcmProcessorBase{

		public new const string Name = "�e�X�g�v���Z�b�T";
		public new const string Description = "ECCM�v���Z�b�T�̃e�X�g�p�ł��B�P��5�b�҂��A�������܂���B";

// �R���X�g���N�^
		public TestProcessor(EcmProject proj) : base(proj){}

// public ���\�b�h

		// �^����ꂽ EcmItem �ɑΉ�����t�@�C���� Parse ���Ēu�����܂��B
		public override ProcessResult Process(EcmItem targetItem){
			Log.AddInfo("ID: {0} �����J�n", targetItem.Id);
			Thread.Sleep(5000);
			Log.AddInfo("ID: {0} �����I��", targetItem.Id);

			ProcessResult result = new ProcessResult();
			return result;
		}

	}


}




