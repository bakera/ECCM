using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Configuration;

namespace Bakera.Eccm{
	public class EcmProjectManager{

		private string myProjectDir;
		private static Dictionary<string, EcmProject> myProjectList = new Dictionary<string, EcmProject>();

// �R���X�g���N�^

		public EcmProjectManager(){
			myProjectDir = WebConfigurationManager.AppSettings[Eccm.ProjectDirKey];
		}

// �v���p�e�B

		public string ProjectDir{
			get{return myProjectDir;}
			set{myProjectDir = value;}
		}

// �C���f�N�T

		public EcmProject this[string projectId]{
			get{
				return GetProject(projectId);
			}
		}


// ���\�b�h

		// �n���ꂽ ID �ɑΉ����� Setting ���擾���܂��B
		public Setting GetSetting(string projectId){
			string projectXmlFile = GetXmlPath(projectId);
			if(!File.Exists(projectXmlFile)){
				return null;
			}
			Setting s = Setting.GetSetting(projectXmlFile);
			return s;
		}

		// �n���ꂽ ID �ɑΉ����� EcmProject ���擾���܂��B
		public EcmProject GetProject(string projectId){

			// �v���W�F�N�g�f�[�^�̃L���b�V��������A�ŐV�Ȃ炻���Ԃ�
			// �f�[�^�t�@�C�����ŐV�A���ݒ肪�ŐV�ł���΃L���b�V�����g�p���Ă悢
			EcmProject result = GetProjectCache(projectId);
			if(result != null) return result;

			// �L���b�V�����g���Ȃ��ꍇ
			Setting s = GetSetting(projectId);
			if(s == null) return null;
			EcmProject newResult = new EcmProject(this, s);
			myProjectList[projectId] = newResult;
			return newResult;
		}


		// �v���W�F�N�g�f�[�^�̗L���ȃL���b�V��������ΕԂ��܂��B
		// �f�[�^���ŐV�A���ݒ肪�ŐV�ł���΃L���b�V�����g�p���ėǂ��Ɣ��f���܂��B
		// �L���b�V�����g�p�ł��Ȃ��ꍇ�� null ��Ԃ��܂��B
		public EcmProject GetProjectCache(string projectId){
			// �L���b�V�������݂��邩?
			if(!myProjectList.ContainsKey(projectId)) return null;
			EcmProject result = myProjectList[projectId];
			if(result == null) return null;

			// �f�[�^���ŐV��?
			// �f�[�^�t�@�C���̍X�V�����f�[�^�̍X�V�����V�����ꍇ�̓L���b�V���g�p�s��
			if(result.FileTime == default(DateTime)) return null;
			if(result.FileTime >= result.DataTime) return null;

			// �ݒ�t�@�C�����ŐV��?
			// �ݒ�t�@�C���̍X�V�����f�[�^�̍X�V�����V�����ꍇ�̓L���b�V���g�p�s��
			result.Setting.BaseFile.Refresh();
			if(result.Setting.BaseFile.LastWriteTime >= result.DataTime) return null;
			
			
			return result;
		}


		// ���[�h����Ă���S�Ă� EcmProject ���擾���܂��B
		public EcmProject[] GetAllProject(){
			EcmProject[] result = new EcmProject[myProjectList.Values.Count];
			myProjectList.Values.CopyTo(result, 0);
			return result;
		}

		// �v���W�F�N�g�f�B���N�g�����������A�S�Ă� EcmProject �����[�h���܂��B
		public void LoadAllProject(){
			string[] projectSubDirs = Directory.GetDirectories(myProjectDir);
			if(projectSubDirs.Length == 0) return;
			foreach(string dirPath in projectSubDirs){
				string projId = Path.GetFileNameWithoutExtension(dirPath);
				Setting s = this.GetSetting(projId);
				if(s == null) continue;
				GetProject(projId);
			}
		}


		// �v���W�F�N�gID����A�Ή�����v���W�F�N�g�ݒ� XML �t�@�C�������擾���܂��B
		private string GetXmlPath(string id){
			string dir = myProjectDir.TrimEnd('/', '\\');
			return string.Format("{0}\\{1}\\{1}.xml", dir, id);
		}


	}
}




