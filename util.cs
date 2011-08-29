using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Bakera.Eccm{
	public static class Util{

		public static Encoding SjisEncoding{
			get{return Encoding.GetEncoding("Shift_JIS");}

		}

		// �t�@�C������f�[�^��ǂݍ���
		public static string LoadFile(string targetFile, Encoding enc){
			string result = null;

			using(FileStream fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, enc)){
					result = sr.ReadToEnd();
					sr.Close();
				}
				fs.Close();
			}
			return result;
		}

		// Shift_JIS �̃t�@�C������f�[�^��ǂݍ��݂܂��B
		public static string LoadFile(string targetFile){
			return LoadFile(targetFile, SjisEncoding);
		}

		// �t�@�C���Ƀf�[�^���������݂܂��B
		public static void WriteFile(string targetFile, string data, Encoding enc){
			using(FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None)){
				using(StreamWriter sw = new StreamWriter(fs, enc)){
					sw.Write(data);
					sw.Close();
				}
				fs.Close();
			}
		}

		// �t�@�C���Ƀf�[�^�� Shift_JIS �Ƃ��ď������݂܂��B
		public static void WriteFile(string targetFile, string data){
			WriteFile(targetFile, data, SjisEncoding);
		}

		// �t�@�C���Ƀf�[�^��ǉ��������݂��܂��B
		public static void AppendWriteFile(string targetFile, string data, Encoding enc){
			using(FileStream fs = new FileStream(targetFile, FileMode.Append, FileAccess.Write, FileShare.None)){
				using(StreamWriter sw = new StreamWriter(fs, enc)){
					sw.Write(data);
					sw.Close();
				}
				fs.Close();
			}
		}

		// �t�@�C���Ƀf�[�^�� Shift_JIS �Ƃ��ď������݂܂��B
		public static void AppendWriteFile(string targetFile, string data){
			AppendWriteFile(targetFile, data, SjisEncoding);
		}


		// ��O���X���[���܂��B
		public static void Throw(params object[] objs){
			string[] result = new string[objs.Length];
			for(int i=0; i < objs.Length; i++){
				Object o = objs[i];
				if(o == null){
					result[i] = "[null]";
				} else {
					result[i] = o.ToString();
				}
			}
			throw new Exception(String.Join(", ", result));
		}

		// Encoding ���擾���܂��B
		// UTF-8 �̏ꍇ�̓o�C�g�I�[�_�[�}�[�N�����Ȃ����̂�Ԃ��܂��B
		public static Encoding GetEncoding(string s){
			if(s.Equals("UTF-8", StringComparison.CurrentCultureIgnoreCase)){
				return new UTF8Encoding(false);
			}
			return Encoding.GetEncoding(s);
		}


// �����񑀍�
		// ������̉E�[����w�肳�ꂽ��������폜���܂��B
		public static string CutRight(this string src, string cut){
			if(src == null) return null;
			if(string.IsNullOrEmpty(cut) || !src.EndsWith(cut)) return src;
			int destLength = src.Length - cut.Length;
			return src.Remove(destLength);
		}

		// ������̍��[����w�肳�ꂽ��������폜���܂��B
		public static string CutLeft(this string src, string cut){
			if(src == null) return null;
			if(cut == null || !src.StartsWith(cut)) return src;
			return src.Remove(0, cut.Length);
		}

		// �t�B�[���h�̒l���擾���܂��B
		public static string GetFieldValue(Type t, string name){
			Object o = t.GetField(name).GetValue(null);
			if(o == null) return null;
			return o.ToString();
		}

		// ������𐔒l�ɂ��܂��B
		public static int ToInt32(this object o){
			int result = 0;
			Int32.TryParse(o.ToString(), out result);
			return result;
		}


// XML����

		public static void AddClass(this XmlElement e, string className){
			if(e == null) return;
			string prevClass = e.GetAttribute("class");
			if(string.IsNullOrEmpty(prevClass)){
				e.SetAttribute("class", className);
			} else {
				e.SetAttribute("class", prevClass + " " + className);
			}
		}


	}

}




