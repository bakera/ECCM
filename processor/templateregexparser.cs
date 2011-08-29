using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Bakera.Eccm{
	public class TemplateRegexParser{
		
		private EcmProject myProject;
		private EcmLog myLog;
		private string myTemplateName;

		private static Regex PlaceHolderMarkRegex = new Regex("{[a-z]+\\+?}");
		private Dictionary<string, PlaceHolder> myPlaceHolderSets = new Dictionary<string, PlaceHolder>(); // �v���[�X�z���_�̑g�ݍ��킹
		private List<PlaceHolder> myPlaceHolderList = new List<PlaceHolder>(); // �e���v���[�g����L���v�`�������v���[�X�z���_�̃��X�g
		private GroupCollection myCaptureDataList; // �R���e���c����L���v�`�������f�[�^�̃��X�g
		private int myReplaceCount;


// �v���p�e�B
		public EcmLog Log{
			get{return myLog;}
		}


// �R���X�g���N�^
		public TemplateRegexParser(EcmProject proj, EcmLog log, string templateName){
			myProject = proj;
			myLog = log;
			myTemplateName = templateName;

			myPlaceHolderSets["{num}"] = new PlaceHolder("num", "-?[0-9]*", "-100");
			myPlaceHolderSets["{num+}"] = new PlaceHolder("num", "-?[0-9]+", "-100");
			myPlaceHolderSets["{uri}"] = new PlaceHolder("uri", "[-._~;?:@&=*+&,/#\\[\\]()a-zA-Z0-9]*", "#");
			myPlaceHolderSets["{uri+}"] = new PlaceHolder("uri", "[-._~;?:@&=*+&,/#\\[\\]()a-zA-Z0-9]*", "#");
			myPlaceHolderSets["{text}"] = new PlaceHolder("text", "[^\\n<]*", "�e�L�X�g�e�L�X�g�e�L�X�g");
			myPlaceHolderSets["{text+}"] = new PlaceHolder("text", "[^\\n<]+", "�e�L�X�g�e�L�X�g�e�L�X�g");
			myPlaceHolderSets["{any}"] = new PlaceHolder("any", ".*", "??????????");
			myPlaceHolderSets["{any+}"] = new PlaceHolder("any", ".+", "??????????");
		}


		// �e���v���[�g�t�@�C���� Regex �p�[�X
		public string Parse(MarkedData md, string templateData){
			// ���݂̃f�[�^���L���v�`��
			myCaptureDataList = GetCapture(templateData, md.InnerData);
			if(myCaptureDataList == null){
				Log.AddWarning("�e���v���[�g�Ɏg�p���錻�݃f�[�^�̃L���v�`�����ł��܂���ł����B");
				return CreateSample(templateData);
			}
			return CreateResult(templateData);
		}

		private string CreateResult(string templateData){
			myReplaceCount = 1;
			string result = PlaceHolderMarkRegex.Replace(templateData, new MatchEvaluator(PlaceHolderDataReplace));
			Log.AddInfo("�e���v���[�g�̒u�����������܂����B");
			return result;
		}

		private string CreateSample(string templateData){
			string result = PlaceHolderMarkRegex.Replace(templateData, new MatchEvaluator(PlaceHolderSampleReplace));
			Log.AddInfo("�e���v���[�g�ɃT���v���f�[�^���o�͂��܂����B");
			return result;
		}

		// ���݂̃R���e���c����A�v���[�X�z���_�ɑ�������f�[�^���擾���܂��B
		private GroupCollection GetCapture(string templateData, string contentData){
			// �e���v���[�g����� ������ {} ��T���Đ��K�\���ɒu�����APlaceHolder �̔z����쐬
			Log.AddInfo("�e���v���[�g�����");
			string replacedTemplate = "^\\s*" + PlaceHolderMarkRegex.Replace(templateData, new MatchEvaluator(PlaceHolderMarkReplace)) + "\\s*$";
			if(myPlaceHolderList.Count == 0){
				Log.AddInfo("�e���v���[�g�ɂ̓v���[�X�z���_���܂܂�܂���B");
				return null;
			}

			string matchlog = "";
			foreach(PlaceHolder ph in myPlaceHolderList){
				matchlog += ph.Mark;
			}
			Log.AddInfo("�v���[�X�z���_��F�m���܂����B : {0}", matchlog);
			return GetCaptureFromData(replacedTemplate, contentData);
		}




		private GroupCollection GetCaptureFromData(string regexText, string contentData){

			Log.AddInfo("���K�\�� : " + regexText);
			// ���K�\�����쐬
			Regex captureTemplateRegex = null;
			try{
				captureTemplateRegex = new Regex(regexText, RegexOptions.Multiline);
			} catch(Exception e){
				Log.AddError("�f�t�H���g�L���v�`���e���v���[�g���K�\���쐬���̃G���[�ł� : {0}", e.ToString());
				return null;
			}

			// �f�[�^�Ƀ}�b�`������
			Match m = captureTemplateRegex.Match(contentData);
			if(!m.Success){
				Log.AddInfo("�f�[�^���}�b�`���܂���ł����B");
				return null;
			}

			string capturelog = "";
			for(int i=1; i < m.Groups.Count; i++){
				capturelog += "{" + m.Groups[i].Value + "}";
			}

			Log.AddInfo("���݂̃R���e���c����f�[�^���L���v�`�����܂����B�L���v�`��������{0} : {1}", m.Groups.Count-1, capturelog);
			return m.Groups;
		}


		// Regex.Replace �Ŏg�p���邽�߂̊֐�
		// ������ {} ��T���Đ��K�\���ɒu�����APlaceHolder �̔z����쐬
		private string PlaceHolderMarkReplace(Match match){
			string placeHolderMark = match.Value;

			// �o�^����Ă���v���[�X�z���_�łȂ��ꍇ�A�u�����Ȃ��ł��̂܂ܕԂ�
			if(!myPlaceHolderSets.ContainsKey(placeHolderMark)) return placeHolderMark;
			PlaceHolder ph = myPlaceHolderSets[placeHolderMark];
			if(ph == null) return placeHolderMark;

			// �o�^����Ă���ꍇ�A�ǂ̎�ނ̃v���[�X�z���_�Ƀ}�b�`�����̂��o���Ă���
			myPlaceHolderList.Add(ph);
			return ph.Regex;
		}


		private string PlaceHolderDataReplace(Match match){
			return myCaptureDataList[myReplaceCount++].Value.Trim();
		}

		private string PlaceHolderSampleReplace(Match match){
			string placeHolderMark = match.Value;
			PlaceHolder ph = myPlaceHolderSets[placeHolderMark];
			if(ph == null) return placeHolderMark;
			return ph.Data;
		}



// PlaceHolder �N���X

		private class PlaceHolder{
			private string myName;
			private string myPattern;
			private string myData;

			public PlaceHolder(string name, string pattern, string data){
				myName = name;
				myPattern = pattern;
				myData = data;
			}

			public string Name{
				get{return myName;}
			}

			public string Mark{
				get{return "{" + myName + "}";}
			}

			public string Pattern{
				get{return myPattern;}
			}

			public string Regex{
				get{return "(" + myPattern + ")";}
			}

			public string Data{
				get{return myData;}
			}

		}

	}

}




