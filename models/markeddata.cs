using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bakera.Eccm{
	public class MarkedData{

		private static Regex myOpenerReg = new EcmRegex.GeneralOpener();
		private static Regex myTcReg = new EcmRegex.TemplateCloser();
		private static Regex myEcReg = new EcmRegex.ExportCloser();
		private static Regex myPcReg = new EcmRegex.PropertyCloser();

		private string myData;
		private Match myStartMatch;
		private Match myEndMatch;


// コンストラクタ

		private MarkedData(string data, Match sMatch, Match eMatch){
			myData = data;
			myStartMatch = sMatch;
			myEndMatch = eMatch;
		}


// プロパティ

		// マーク種別を示す文字列 (=か+)
		public string MarkKind{
			get{return myStartMatch.Groups[1].Value;}
		}

		// テンプレート名を示す文字列
		public string MarkName{
			get{return myStartMatch.Groups[2].Value;}
		}

		// プロパティの ID を示す文字列
		public string PropertyIdName{
			get{return myStartMatch.Groups[3].Value;}
		}

		// プロパティのパラメータを示す文字列
		public string PropertyParamName{
			get{return myStartMatch.Groups[4].Value;}
		}

		// マークの前の文字列
		public string FrontData{
			get{return myData.Substring(0, myStartMatch.Index);}
		}

		public string StartMark{
			get{return myStartMatch.Value;}
		}

		public string OuterData{
			get{return StartMark + InnerData + EndMark;}
		}

		public string InnerData{
			get{
				if(myEndMatch == null) return null;
				int startPos = myStartMatch.Index + StartMark.Length;
				int endPos = myEndMatch.Index;
				int len = endPos - startPos;
				return myData.Substring(startPos, len);
			}
		}

		public string EndMark{
			get{
				if(myEndMatch == null) return null;
				return myEndMatch.Value;
			}
		}

		public string BackData{
			get{
				if(myEndMatch == null) return myData.Substring(myStartMatch.Index + StartMark.Length);
				return myData.Substring(myEndMatch.Index + EndMark.Length);
			}
		}

		// 全てのデータ
		public string Data{
			get{return myData;}
		}

		public MarkType MarkType{
			get{
				if(myStartMatch.Groups[1].Value == "+") return MarkType.Export;
				if(myStartMatch.Groups[4].Value == "") return MarkType.Template;
				return MarkType.Property;
			}
		}

// メソッド

		public static MarkedData Parse(string data){
			if(string.IsNullOrEmpty(data)) return null;
			Match sMatch = myOpenerReg.Match(data);
			if(!sMatch.Success) return null;

			string templateKind = sMatch.Groups[1].Value;
			string templateName = sMatch.Groups[2].Value;
			string templateIdName = sMatch.Groups[3].Value;
			string templateParam = sMatch.Groups[4].Value;
			string templateEmptyMark = sMatch.Groups[5].Value;

			// 終了マークの種類を判別
			Regex endMarkReg = null;
			if(templateKind == "="){
				// 空要素プロパティ
				if(templateEmptyMark != "") return new MarkedData(data, sMatch, null);

				if(templateParam == ""){
					endMarkReg = myTcReg;
				} else {
					endMarkReg = myPcReg;
				}
			} else {
				endMarkReg = myEcReg;
			}

			// 終了マーク探す
			Match eMatch = endMarkReg.Match(data);
			for(;;){
				if(!eMatch.Success){
					throw new ParserException("{0} に対応する終了マークがありません。", templateName);
				}
				string templateEndName = eMatch.Groups[1].Value;
				// 名前が対応していれば OK(処理終了), そうでない場合は無視して次を探す
				if(templateName == templateEndName) break;
				eMatch = eMatch.NextMatch();
			}

			MarkedData result = new MarkedData(data, sMatch, eMatch);
			return result;
		}

	}

	public enum MarkType{
		Template,
		Property,
		Export
	}

}


