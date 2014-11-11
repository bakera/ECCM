using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace Bakera.Eccm{
	public class ExportManager{


		private Regex myEoReg = new EcmRegex.ExportOpener();
		private Regex myEcReg = new EcmRegex.ExportCloser();
		private Parser myParser = null;
		private EcmItem myItem = null;

		// string ���L�[�AExportObject �� value �Ƃ���n�b�V���e�[�u��
		private Hashtable myTable = new Hashtable();


// �R���X�g���N�^

		public ExportManager(Parser p, EcmItem item){
			myParser = p;
			myItem = item;
		}


// �C���f�N�T
		// ���̖��O�̒l�����o��
		// ����p����
		public string this[string dataname]{
			get{
				ExportObject eo = myTable[dataname] as ExportObject;
				if(eo == null) return null;
				return eo.Get();
			}
		}


// ���\�b�h

		public void Parse(string data){
			while(data.Length > 0){

				// EO ��T���B�Ȃ���ΏI��
				Match sMatch = myEoReg.Match(data);
				if(!sMatch.Success) break;

				// �G�N�X�|�[�g�J�n�̖��O���擾
				string exportName = sMatch.Groups[1].Value;

				// EC ��T���B�Ȃ���΃G���[
				Match eMatch = myEcReg.Match(data);
				string exportEndName = null;
				for(;;){
					if(!eMatch.Success){
						throw new ParserException("�G�N�X�|�[�g�u" + exportName + "�v�ɑΉ�����I���}�[�N������܂���B");
					}
					exportEndName = eMatch.Groups[1].Value;

					// ���O���Ή����Ă���� OK(�����I��), �����łȂ��ꍇ�͖������Ď���T��
					if(exportName == exportEndName) break;
					eMatch = eMatch.NextMatch();
				}

				int startIndex = sMatch.Index + sMatch.Length;
				string exported = data.Substring(startIndex, eMatch.Index - startIndex);

				// �e�[�u���ɒǉ�
				if(myTable[exportName] == null){
					myTable[exportName] = new ExportObject(exported);
					myParser.Log.AddInfo("{0} �G�N�X�|�[�g {1} �̓��e���L�����܂����B(�f�[�^�T�C�Y : {2})", myItem.FqId, exportName, exported.Length);
				} else {
					ExportObject eo = myTable[exportName] as ExportObject;
					eo.Add(exported);
					myParser.Log.AddInfo("{0} �G�N�X�|�[�g {1} �̓��e��ǉ��ŋL�����܂����B({2}���ځA�f�[�^�T�C�Y : {3})", myItem.FqId, exportName, eo.Count, exported.Length);
				}

				// ����q�̃G�N�X�|�[�g��{��
				Parse(exported);
				data = data.Remove(0, eMatch.Index + eMatch.Length);
			}
		}

		public void Print(){
			foreach(string key in myTable.Keys){
				Console.WriteLine(key);
				ExportObject eo = myTable[key] as ExportObject;
				Console.WriteLine(eo.Get());
				Console.WriteLine();
			}
		}


	}
}




