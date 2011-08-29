using System;
using System.Web;
using System.Text;

namespace Bakera.Eccm{
	public class EcmPluginDocument{

		protected StringBuilder myInnerString = new StringBuilder();

// プロパティ

		// 内部の StringBuilder を取得します。
		public StringBuilder InnerString{
			get{return myInnerString;}
		}

// メソッド

		public void Write(object s){
			if(s == null) return;
			myInnerString.Append(s.ToString());
		}

		public void Write(string f, object[] s1){
			myInnerString.AppendFormat(f, s1);
		}

		public void Write(string f, object s1, object s2){
			myInnerString.AppendFormat(f, s1, s2);
		}

		public void WriteLine(){
			myInnerString.AppendLine();
		}

		public void WriteLine(object s){
			if(s == null) return;
			myInnerString.AppendLine(s.ToString());
		}

		public void WriteLine(string f, object s1, object s2){
			myInnerString.AppendFormat(f, s1, s2);
			myInnerString.AppendLine();
		}

		public void WritePCDATA(object s){
			Write(EcmPluginBase.H(s));
		}

		public void WritePCDATA(string f, object s1, params object[] s2){
			Write(f, ConvertArrayString(s1, s2, EcmPluginBase.H));
		}

		public void WriteQuoted(object s){
			Write(EcmPluginBase.Q(s));
		}

		public void WriteQuoted(string f, object s1, params object[] s2){
			string[] datas = ConvertArrayString(s1, s2, EcmPluginBase.Q);
			Write(f, datas);
		}

		public override string ToString(){
			return myInnerString.ToString();
		}

		// テキストをクリアします。
		public void Clear(){
			myInnerString = new StringBuilder();
		}

// プライベートメソッド
		private delegate string StrConvAction(object source);

		private static string[] ConvertArrayString(object[] args, StrConvAction action){
			string[] result = new string[args.Length];
			for(int i=0; i < args.Length; i++){
				result[i] = action(args[i]);
			}
			return result;
		}

		private static string[] ConvertArrayString(object o, object[] args, StrConvAction action){
			string[] result = new string[args.Length+1];
			result[0] = action(o);
			for(int i=0; i < args.Length; i++){
				result[i+1] = action(args[i]);
			}
			return result;
		}

	}
}

