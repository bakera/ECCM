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
		private Dictionary<string, PlaceHolder> myPlaceHolderSets = new Dictionary<string, PlaceHolder>(); // プレースホルダの組み合わせ
		private List<PlaceHolder> myPlaceHolderList = new List<PlaceHolder>(); // テンプレートからキャプチャしたプレースホルダのリスト
		private GroupCollection myCaptureDataList; // コンテンツからキャプチャしたデータのリスト
		private int myReplaceCount;


// プロパティ
		public EcmLog Log{
			get{return myLog;}
		}


// コンストラクタ
		public TemplateRegexParser(EcmProject proj, EcmLog log, string templateName){
			myProject = proj;
			myLog = log;
			myTemplateName = templateName;

			myPlaceHolderSets["{num}"] = new PlaceHolder("num", "-?[0-9]*", "-100");
			myPlaceHolderSets["{num+}"] = new PlaceHolder("num", "-?[0-9]+", "-100");
			myPlaceHolderSets["{uri}"] = new PlaceHolder("uri", "[-._~;?:@&=*+&,/#\\[\\]()a-zA-Z0-9]*", "#");
			myPlaceHolderSets["{uri+}"] = new PlaceHolder("uri", "[-._~;?:@&=*+&,/#\\[\\]()a-zA-Z0-9]*", "#");
			myPlaceHolderSets["{text}"] = new PlaceHolder("text", "[^\\n<]*", "テキストテキストテキスト");
			myPlaceHolderSets["{text+}"] = new PlaceHolder("text", "[^\\n<]+", "テキストテキストテキスト");
			myPlaceHolderSets["{any}"] = new PlaceHolder("any", ".*", "??????????");
			myPlaceHolderSets["{any+}"] = new PlaceHolder("any", ".+", "??????????");
		}


		// テンプレートファイルの Regex パース
		public string Parse(MarkedData md, string templateData){
			// 現在のデータをキャプチャ
			myCaptureDataList = GetCapture(templateData, md.InnerData);
			if(myCaptureDataList == null){
				Log.AddWarning("テンプレートに使用する現在データのキャプチャができませんでした。");
				return CreateSample(templateData);
			}
			return CreateResult(templateData);
		}

		private string CreateResult(string templateData){
			myReplaceCount = 1;
			string result = PlaceHolderMarkRegex.Replace(templateData, new MatchEvaluator(PlaceHolderDataReplace));
			Log.AddInfo("テンプレートの置換が完了しました。");
			return result;
		}

		private string CreateSample(string templateData){
			string result = PlaceHolderMarkRegex.Replace(templateData, new MatchEvaluator(PlaceHolderSampleReplace));
			Log.AddInfo("テンプレートにサンプルデータを出力しました。");
			return result;
		}

		// 現在のコンテンツから、プレースホルダに相当するデータを取得します。
		private GroupCollection GetCapture(string templateData, string contentData){
			// テンプレートを解析 頭から {} を探して正規表現に置換しつつ、PlaceHolder の配列を作成
			Log.AddInfo("テンプレートを解析");
			string replacedTemplate = "^\\s*" + PlaceHolderMarkRegex.Replace(templateData, new MatchEvaluator(PlaceHolderMarkReplace)) + "\\s*$";
			if(myPlaceHolderList.Count == 0){
				Log.AddInfo("テンプレートにはプレースホルダが含まれません。");
				return null;
			}

			string matchlog = "";
			foreach(PlaceHolder ph in myPlaceHolderList){
				matchlog += ph.Mark;
			}
			Log.AddInfo("プレースホルダを認知しました。 : {0}", matchlog);
			return GetCaptureFromData(replacedTemplate, contentData);
		}




		private GroupCollection GetCaptureFromData(string regexText, string contentData){

			Log.AddInfo("正規表現 : " + regexText);
			// 正規表現を作成
			Regex captureTemplateRegex = null;
			try{
				captureTemplateRegex = new Regex(regexText, RegexOptions.Multiline);
			} catch(Exception e){
				Log.AddError("デフォルトキャプチャテンプレート正規表現作成時のエラーです : {0}", e.ToString());
				return null;
			}

			// データにマッチさせる
			Match m = captureTemplateRegex.Match(contentData);
			if(!m.Success){
				Log.AddInfo("データがマッチしませんでした。");
				return null;
			}

			string capturelog = "";
			for(int i=1; i < m.Groups.Count; i++){
				capturelog += "{" + m.Groups[i].Value + "}";
			}

			Log.AddInfo("現在のコンテンツからデータをキャプチャしました。キャプチャした数{0} : {1}", m.Groups.Count-1, capturelog);
			return m.Groups;
		}


		// Regex.Replace で使用するための関数
		// 頭から {} を探して正規表現に置換しつつ、PlaceHolder の配列を作成
		private string PlaceHolderMarkReplace(Match match){
			string placeHolderMark = match.Value;

			// 登録されているプレースホルダでない場合、置換しないでそのまま返す
			if(!myPlaceHolderSets.ContainsKey(placeHolderMark)) return placeHolderMark;
			PlaceHolder ph = myPlaceHolderSets[placeHolderMark];
			if(ph == null) return placeHolderMark;

			// 登録されている場合、どの種類のプレースホルダにマッチしたのか覚えておく
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



// PlaceHolder クラス

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




