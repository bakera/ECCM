using System;
using System.Collections.Generic;


namespace Bakera.Eccm{
	public class EcmLog{

		private List<EcmLogItem> myMessages = new List<EcmLogItem>();
		private EcmErrorLevel myErrorLevel = EcmErrorLevel.Unknown;
		private EcmErrorLevel myMinimumErrorLevel = EcmErrorLevel.Unknown;

// public �v���p�e�B

		// �G���[���x�����擾���܂��B
		public EcmErrorLevel ErrorLevel{
			get{return myErrorLevel;}
		}

		// �Œ�G���[���x����ݒ�E�擾���܂��B
		// ���̃��x�������̃G���[�͋L�^���܂���B
		public EcmErrorLevel MinimumErrorLevel{
			get{return myMinimumErrorLevel;}
			set{myMinimumErrorLevel = value;}
		}


// public ���\�b�h

// Add
		// ���b�Z�[�W��ǉ����܂��B
		public void AddInfo(string message){
			Add(message, EcmErrorLevel.Information);
		}
		// �t�H�[�}�b�g��������w�肵�āA���b�Z�[�W��ǉ����܂��B
		public void AddInfo(string format, params object[] messages){
			AddInfo(string.Format(format, messages));
		}

		// �d�v���b�Z�[�W��ǉ����܂��B
		public void AddImportant(string message){
			Add(message, EcmErrorLevel.Important);
		}
		// �t�H�[�}�b�g��������w�肵�āA�d�v���b�Z�[�W��ǉ����܂��B
		public void AddImportant(string format, params object[] messages){
			AddImportant(string.Format(format, messages));
		}

		// ���Ӄ��b�Z�[�W��ǉ����܂��B
		public void AddWarning(string message){
			Add(message, EcmErrorLevel.Warning);
		}
		// �t�H�[�}�b�g��������w�肵�āA���Ӄ��b�Z�[�W��ǉ����܂��B
		public void AddWarning(string format, params object[] messages){
			AddWarning(string.Format(format, messages));
		}

		// �x�����b�Z�[�W��ǉ����܂��B
		public void AddAlert(string message){
			Add(message, EcmErrorLevel.Alert);
		}
		// �t�H�[�}�b�g��������w�肵�āA�x�����b�Z�[�W��ǉ����܂��B
		public void AddAlert(string format, params object[] messages){
			AddAlert(string.Format(format, messages));
		}

		// �G���[���b�Z�[�W��ǉ����܂��B
		public void AddError(string message){
			Add(message, EcmErrorLevel.Error);
		}
		// �t�H�[�}�b�g��������w�肵�āA�G���[���b�Z�[�W��ǉ����܂��B
		public void AddError(string format, params object[] messages){
			AddError(string.Format(format, messages));
		}

		// �f�o�b�O���b�Z�[�W��ǉ����܂��B
		public void AddDebug(string message){
			Add(message, EcmErrorLevel.Debug);
		}
		// �t�H�[�}�b�g��������w�肵�āA�f�o�b�O���b�Z�[�W��ǉ����܂��B
		public void AddDebug(string format, params object[] messages){
			AddDebug(string.Format(format, messages));
		}

		// ���b�Z�[�W�Ǝ�ނ��w�肵�āA���b�Z�[�W��ǉ����܂��B
		public void Add(string message, EcmErrorLevel level){
			if(level < myMinimumErrorLevel) return;
			EcmLogItem eli = new EcmLogItem(message, level);
			Add(eli);
		}

		// ���b�Z�[�W��ǉ����܂��B
		public void Add(EcmLogItem el){
			if(el.Kind > myErrorLevel) myErrorLevel = el.Kind;
			myMessages.Add(el);
		}

		// �ʂ̃��O���������܂��B
		public void Append(EcmLog log){
			foreach(EcmLogItem el in log.GetAll()){
				Add(el);
			}
		}


// �o��

		// ���ׂẴ��b�Z�[�W���o�͂��܂��B
		public EcmLogItem[] GetAll(){
			EcmLogItem[] result = new EcmLogItem[myMessages.Count];
			myMessages.CopyTo(result, 0);
			return result;
		}


		// ���ׂẴ��b�Z�[�W�𕶎���Ƃ��ďo�͂��܂��B
		public override string ToString(){
			string result = "";
			foreach(EcmLogItem eli in myMessages){
				result += eli.ToString();
				result += "\n";
			}
			return result;
		}

	}


}

