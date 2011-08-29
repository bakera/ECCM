using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bakera.Eccm{
	public class EcmTemplate{

// �R���X�g���N�^
		private Setting mySetting;
		private EcmProject myProject;
		private string myName;
		private FileInfo myFile;
		private FileInfo myBackupFile;
		private const string TemplateBackupDirName = "template_backup";

		public EcmTemplate(EcmProject proj, string templateName){
			myProject = proj;
			mySetting = proj.Setting;
			myName = templateName;

			string templateFileName = mySetting.TemplateFullPath.FullName.TrimEnd('\\') + '\\' + templateName + '.' + mySetting.TemplateExt.TrimStart('.');
			myFile = new FileInfo(templateFileName);
			myBackupFile = new FileInfo(GetBackupPath());
		}


// �v���p�e�B
		public string Name{
			get{return myName;}
		}

		public FileInfo File{
			get{return myFile;}
		}

		public bool Exists{
			get{return myFile.Exists;}
		}

// ���\�b�h

		// �f�[�^�����̂܂܎擾���܂��B
		public string GetData(){
			if(!myFile.Exists) return null;
			string result = Util.LoadFile(myFile.FullName, mySetting.HtmlEncodingObj);
			return result;
		}

		// �o�b�N�A�b�v�t�@�C���𒲍����A�K�v�ȃo�b�N�A�b�v���쐬���܂��B
		public void Backup(){
			FileInfo currentBackupFile = new FileInfo(GetBackupPath());
			// �Ȃ�������R�s�[���Ă���
			if(!currentBackupFile.Exists){
				try{
					currentBackupFile.Directory.Create();
					myFile.CopyTo(currentBackupFile.FullName, true);
				}catch{}
				return;
			}

			// �o�b�N�A�b�v�t�@�C��������̂Ń^�C���X�^���v���r
			// �����Ȃ牽�����Ȃ�
			if(File.LastWriteTime == currentBackupFile.LastWriteTime) return;

			// �O��o�b�N�A�b�v���X�V
			FileInfo prevBackupFile = new FileInfo(currentBackupFile.FullName + ".prev");
			currentBackupFile.CopyTo(prevBackupFile.FullName, true);

			// ���݃o�b�N�A�b�v���X�V
			myFile.CopyTo(currentBackupFile.FullName, true);
		}


// �v���C�x�[�g���\�b�h

		// �o�b�N�A�b�v�̃p�X���擾����
		private string GetBackupPath(){
			string templateFilePath = mySetting.BaseDir.FullName.TrimEnd('\\') + '\\' + TemplateBackupDirName + '\\' + myName + '.' + mySetting.TemplateExt.TrimStart('.');
			return templateFilePath;
		}




	}
}


