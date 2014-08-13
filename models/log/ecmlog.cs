using System;
using System.Collections.Generic;


namespace Bakera.Eccm{
	public class EcmLog{

		private List<EcmLogItem> myMessages = new List<EcmLogItem>();
		private EcmErrorLevel myErrorLevel = EcmErrorLevel.Unknown;
		private EcmErrorLevel myMinimumErrorLevel = EcmErrorLevel.Unknown;

// public プロパティ

		// エラーレベルを取得します。
		public EcmErrorLevel ErrorLevel{
			get{return myErrorLevel;}
		}

		// 最低エラーレベルを設定・取得します。
		// このレベル未満のエラーは記録しません。
		public EcmErrorLevel MinimumErrorLevel{
			get{return myMinimumErrorLevel;}
			set{myMinimumErrorLevel = value;}
		}


// public メソッド

// Add
		// メッセージを追加します。
		public void AddInfo(string message){
			Add(message, EcmErrorLevel.Information);
		}
		// フォーマット文字列を指定して、メッセージを追加します。
		public void AddInfo(string format, params object[] messages){
			AddInfo(string.Format(format, messages));
		}

		// 重要メッセージを追加します。
		public void AddImportant(string message){
			Add(message, EcmErrorLevel.Important);
		}
		// フォーマット文字列を指定して、重要メッセージを追加します。
		public void AddImportant(string format, params object[] messages){
			AddImportant(string.Format(format, messages));
		}

		// 注意メッセージを追加します。
		public void AddWarning(string message){
			Add(message, EcmErrorLevel.Warning);
		}
		// フォーマット文字列を指定して、注意メッセージを追加します。
		public void AddWarning(string format, params object[] messages){
			AddWarning(string.Format(format, messages));
		}

		// 警告メッセージを追加します。
		public void AddAlert(string message){
			Add(message, EcmErrorLevel.Alert);
		}
		// フォーマット文字列を指定して、警告メッセージを追加します。
		public void AddAlert(string format, params object[] messages){
			AddAlert(string.Format(format, messages));
		}

		// エラーメッセージを追加します。
		public void AddError(string message){
			Add(message, EcmErrorLevel.Error);
		}
		// フォーマット文字列を指定して、エラーメッセージを追加します。
		public void AddError(string format, params object[] messages){
			AddError(string.Format(format, messages));
		}

		// デバッグメッセージを追加します。
		public void AddDebug(string message){
			Add(message, EcmErrorLevel.Debug);
		}
		// フォーマット文字列を指定して、デバッグメッセージを追加します。
		public void AddDebug(string format, params object[] messages){
			AddDebug(string.Format(format, messages));
		}

		// メッセージと種類を指定して、メッセージを追加します。
		public void Add(string message, EcmErrorLevel level){
			if(level < myMinimumErrorLevel) return;
			EcmLogItem eli = new EcmLogItem(message, level);
			Add(eli);
		}

		// メッセージを追加します。
		public void Add(EcmLogItem el){
			if(el.Kind > myErrorLevel) myErrorLevel = el.Kind;
			myMessages.Add(el);
		}

		// 別のログを結合します。
		public void Append(EcmLog log){
			foreach(EcmLogItem el in log.GetAll()){
				Add(el);
			}
		}


// 出力

		// すべてのメッセージを出力します。
		public EcmLogItem[] GetAll(){
			EcmLogItem[] result = new EcmLogItem[myMessages.Count];
			myMessages.CopyTo(result, 0);
			return result;
		}


		// すべてのメッセージを文字列として出力します。
		public override string ToString(){
			string result = "";
			foreach(EcmLogItem eli in myMessages){
				result += eli.ToString();
				result += "\n";
			}
			return result;
		}

	}


}

