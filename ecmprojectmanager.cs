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
			// �f�[�^�������āA�ŐV�Ȃ炻���Ԃ�
			if(myProjectList.ContainsKey(projectId)){
				EcmProject result = myProjectList[projectId];
				if(result != null && result.FileTime != default(DateTime) && result.FileTime == result.DataTime) return result;
			}
			Setting s = GetSetting(projectId);
			if(s == null) return null;
			EcmProject newResult = new EcmProject(this, s);
			myProjectList[projectId] = newResult;
			return newResult;
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




