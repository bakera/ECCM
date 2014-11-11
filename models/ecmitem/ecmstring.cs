using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Web;


namespace Bakera.Eccm{


	// EcmItem�Ȃǂ̊��N���X
	public class EcmString{

		protected string myId = null;
		protected EcmProject myProject = null;

// �R���X�g���N�^

		// ID �� EcmProject ���� EcmItem ���쐬���܂��B
		public EcmString(string id, EcmProject proj) : this (proj){
			myId = id;
		}

		public EcmString(EcmProject proj){
			myProject = proj;
		}


// public �v���p�e�B

		// ��������Parser���擾���܂��B
		public Parser Parser{
			get; set;
		}

		// �e�� EcmProject ���擾���܂��B
		public EcmProject Project{
			get{ return myProject; }
		}

		// ID ���擾���܂��B
		public string Id{
			get{return myId;}
			set{myId = value;}
		}

		// ���S�C�����ꂽ ID ���擾���܂��B
		public virtual string FqId{
			get{return myId;}
		}

		// ��������index.html�Ȃǂ���菜������������擾���܂��B
		public virtual string WithoutIndex{
			get{
				return Util.CutRight(myId, Project.Setting.IndexLinkSuffix);
			}
		}


// public ���\�b�h

		public override string ToString(){
			return myId;
		}

		public string ToUpper(){
			return myId.ToUpper();
		}

		public string ToLower(){
			return myId.ToLower();
		}

		public string HtmlEncode(){
			return HttpUtility.HtmlEncode(myId);
		}

		public string Format(string s){
			return String.Format(s, myId);
		}

		public string Replace(string s){
			string[] replaceParams = s.Split(',');
			if(replaceParams.Length == 0) return myId;
			if(replaceParams.Length == 1){
				return myId.Replace(replaceParams[0], "");
			}
			return myId.Replace(replaceParams[0], replaceParams[1]);
		}


		public string Truncate(string s){
			try{
				int len = Convert.ToInt32(s);
				return myId.Substring(0, len);
			} catch {
				return null;
			}
		}
		
		public string UrlEncode(){
			return UrlEncode(myId);
		}

		public string UrlEncode(string s){
			return System.Web.HttpUtility.UrlEncode(s);
		}

		// Parser�����݂���Ƃ��A���̃e�L�X�g��Parse���܂��B
		public string Parse(){
			return Parse(myId);
		}

		// Parser�����݂���Ƃ��A�n���ꂽ�e�L�X�g��Parse���܂��B
		public string Parse(string data){
			return Parser.GeneralParse(data);
		}


		// Parser�����݂���Ƃ��A�e���v���[�g��K�p���܂��B
		public string ApplyTemplate(string templateName){
			string mark = string.Format("<!--={0}/-->", templateName);
			return Parse(mark);
		}




// �ÓI���\�b�h

		// �^����ꂽ����������̂܂ܕԂ��܂��B
		public static string Str(string s){
			return s;
		}

		// FileInfo �𖼑O�Ń\�[�g���邽�߂̔�r���\�b�h�ł��B
		public static int CompareFileInfoByName(FileInfo x, FileInfo y){
			if(x == null){
				if(y == null) return 0;
				return -1;
			}
			if(y == null) return 1;
			return String.Compare(x.Name, y.Name);
		}



	}
}

