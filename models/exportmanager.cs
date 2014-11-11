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

		// string をキー、ExportObject を value とするハッシュテーブル
		private Hashtable myTable = new Hashtable();


// コンストラクタ

		public ExportManager(Parser p, EcmItem item){
			myParser = p;
			myItem = item;
		}


// インデクサ
		// その名前の値を取り出す
		// 副作用あり
		public string this[string dataname]{
			get{
				ExportObject eo = myTable[dataname] as ExportObject;
				if(eo == null) return null;
				return eo.Get();
			}
		}


// メソッド

		public void Parse(string data){
			while(data.Length > 0){

				// EO を探す。なければ終了
				Match sMatch = myEoReg.Match(data);
				if(!sMatch.Success) break;

				// エクスポート開始の名前を取得
				string exportName = sMatch.Groups[1].Value;

				// EC を探す。なければエラー
				Match eMatch = myEcReg.Match(data);
				string exportEndName = null;
				for(;;){
					if(!eMatch.Success){
						throw new ParserException("エクスポート「" + exportName + "」に対応する終了マークがありません。");
					}
					exportEndName = eMatch.Groups[1].Value;

					// 名前が対応していれば OK(処理終了), そうでない場合は無視して次を探す
					if(exportName == exportEndName) break;
					eMatch = eMatch.NextMatch();
				}

				int startIndex = sMatch.Index + sMatch.Length;
				string exported = data.Substring(startIndex, eMatch.Index - startIndex);

				// テーブルに追加
				if(myTable[exportName] == null){
					myTable[exportName] = new ExportObject(exported);
					myParser.Log.AddInfo("{0} エクスポート {1} の内容を記憶しました。(データサイズ : {2})", myItem.FqId, exportName, exported.Length);
				} else {
					ExportObject eo = myTable[exportName] as ExportObject;
					eo.Add(exported);
					myParser.Log.AddInfo("{0} エクスポート {1} の内容を追加で記憶しました。({2}件目、データサイズ : {3})", myItem.FqId, exportName, eo.Count, exported.Length);
				}

				// 入れ子のエクスポートを捜索
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




