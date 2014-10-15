using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Bakera.Eccm{
	public class Parser : EcmProcessorBase{

		private Regex myMsReg = new EcmRegex.MemberSelector();
		private Regex myMetaCharsetReg = new EcmRegex.MetaCharset();

		private ExportManager myExp = null;
		private int myDepthCounter = 0;//無限ループ検出用
		private string myReadData = null;
		private Dictionary<Type, int> myPluginProcessCounter = new Dictionary<Type, int>();

		public new const string Name = "ECCMパーサー";
		public new const string Description = "ECCMコメントのパースと置換を行い、コンテンツをpublishします。";

// コンストラクタ
		public Parser(EcmProject proj) : base(proj){}


// プロパティ

		// パース前の読み取ったデータを取得します。
		public string ReadData{
			get{return myReadData;}
		}


// public メソッド


		// 与えられた EcmItem を、与えられた FileStream からデータを読み取ってパースし、結果の文字列を返します。
		private string Parse(EcmItem targetItem, FileInfo targetFile){

			string result = null;
			// パース開始
			try{
				Log.AddInfo("{0} パース開始", targetItem);
				myDepthCounter = 0;
				if(targetItem == null){
					Log.AddError("{0} は null です。", targetItem);
					return null;
				}
				Project.CurrentItem = targetItem;
				Encoding enc = Setting.HtmlEncodingObj;
				myExp = new ExportManager(this, targetItem);

				if(targetFile != null){
					Log.AddInfo("文字符号化方式「{0}」としてデータ読み取りを開始します。", enc.EncodingName);
					try{
						myReadData = LoadFile(targetFile, enc);
						Log.AddInfo("データを読み取りました。(データサイズ : {0})", myReadData.Length);
					} catch(IOException e){
						Log.AddError("ファイル {0} を開けません : {1}", targetItem.FilePath, e.Message);
						return result;
					} catch(UnauthorizedAccessException e){
						Log.AddError("ファイル {0} を開けません : {1}", targetItem.FilePath, e.Message);
						return result;
					}

					if(Setting.HtmlEncodingDetectFromMetaCharset){
						Log.AddInfo("meta charsetによる文字コード自動判別を行います。");
						Encoding detectedEnc = DetectEncodingFromMetaCharset(myReadData);
						if(detectedEnc == null){
							Log.AddError("meta charsetを検出できなかったため、処理を中断します。");
							return null;
						} else {
							if(detectedEnc.CodePage != enc.CodePage){
								Log.AddInfo("判別結果は既定の文字コードと異なるため、判別結果に基づいてファイルを開きなおします。");
								myReadData = LoadFile(targetFile, detectedEnc);
								Log.AddInfo("データを読み取りました。(データサイズ : {0})", myReadData.Length);
							} else {
								Log.AddInfo("判別結果は既定の文字コードと一致していました。");
							}
						}
					}
					// エクスポートしておく
					myExp.Parse(myReadData);
					Log.AddInfo("{0} 編集領域エクスポート完了", targetItem);
				} else {
					Log.AddInfo("入力データはありません。");
					myReadData = "";
				}

				if(string.IsNullOrEmpty(targetItem.Template)){
					result = GeneralParse(myReadData, false);
				} else {
					EcmTemplate template = GetTemplate(targetItem.Template);
					if(!template.Exists){
						Log.AddAlert("グローバルテンプレートのデータが取得できませんでした。ファイルが見つかりません。: {0}", template.File.FullName);
						return null;
					}
					Log.AddInfo("グローバルテンプレートのデータを取得しました。ファイル : {0}, サイズ: {1}", template.File.FullName, template.File.Length);
					result = GeneralParse(template.GetData());
				}
				Log.AddInfo("{0} パース完了", targetItem);
			} catch(ParserException e){
				Log.AddError("{0} パースエラー: {1}", targetItem, e.Message);
			} catch(Exception e){
				Log.AddError("{0} の処理中にエラーが発生: {1}", targetItem, e.ToString());
			}
			return result;
		}



		// 与えられた EcmItem に対応するファイルを Parse して置換します。
		public override ProcessResult Process(EcmItem targetItem){

			ProcessResult result = new ProcessResult();

			// プロジェクト情報をログ出力
			Log.AddInfo("Project: {0}, FileTime:{1}, DataTime: {2}", Project.Id, Project.FileTime, Project.DataTime);

			// 安全確認
			string[] errorId = Project.GetDuplicateId();
			if(errorId.Length > 0){
				Log.AddError("ID: {0} が重複しています。", string.Join(", ", errorId));
				return result;
			}
			Log.AddInfo("IDの整合性を確認");

			string[] errorPath = Project.GetDuplicatePath();
			if(errorPath.Length > 0){
				Log.AddError("複数の ID が {0} を参照しています。", string.Join(", ", errorPath));
				return result;
			}
			Log.AddInfo("Pathの整合性を確認");


			Log.AddInfo("ID : {0} の項目を発見", targetItem.Id);

			if(string.IsNullOrEmpty(targetItem.Path)){
				Log.AddError("ID : {0} には Path が設定されていないため、パースできません。", targetItem.Id);
				return result;
			}

			// ファイルがあるか?
			// グローバルテンプレートが指定されている場合はファイルが無くても生成することに注意。
			Log.AddInfo("{0} のファイル : {1}", targetItem.Id, targetItem.FilePath);
			targetItem.File.Refresh();
			if(targetItem.File.Exists){
				// ファイルがある
				// 書き込みできるかどうか (ロックされていないか) 確認する
				try{
					using(FileStream fs = targetItem.File.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None)){
						fs.Close();
					}
					Log.AddInfo("ファイル {0} は存在しますが、書き込み可能なようです。", targetItem.FilePath);
				} catch(IOException e){
					result.AddError("ファイル {0} は書き込みできないようです : {1}", targetItem.FilePath, e.Message);
					Log.AddError("ファイル {0} は書き込みできないようです : {1}", targetItem.FilePath, e.Message);
					return result;
				} catch(UnauthorizedAccessException e){
					result.AddError("ファイル {0} は書き込みできないようです : {1}", targetItem.FilePath, e.Message);
					Log.AddError("ファイル {0} は書き込みできないようです : {1}", targetItem.FilePath, e.Message);
					return result;
				}
				result.Result = Parse(targetItem, targetItem.File);
			} else {
				// ファイルがない
				if(targetItem.Template != null){
					Log.AddInfo("ファイル {0} はありませんが、グローバルテンプレートが指定されています(テンプレート名 : {1})。", targetItem.FilePath, targetItem.Template);
					result.Result = Parse(targetItem, null);
				} else {
					result.AddError("ID : {0} のファイル {1} がありません (グローバルテンプレートも指定されていません)。", targetItem.Id, targetItem.FilePath);
					Log.AddError("ID : {0} のファイル {1} がありません (グローバルテンプレートも指定されていません)。", targetItem.Id, targetItem.FilePath);
					return result;
				}
			}

			// パーサーが null を返したら終了
			if(result.Result == null){
				result.AddError("{0} のパースに失敗しました。パース終了します。", targetItem.Id);
				Log.AddError("{0} のパースに失敗しました。ファイルに書き込まずに終了します。", targetItem.Id);
				return result;
			}

			// パース結果と比較してみる
			if(result.Result == ReadData){
				// 結果が同じなので何もしない
				Log.AddInfo("パース完了しましたが、結果はパース前と変化ありませんでした。");
				result.Message = "パース完了しましたが、結果はパース前と変化ありませんでした。";
			} else {
				// 結果が違うので書き込む
				Log.AddInfo("パース結果はパース前と異なります。ファイルへの書き込みを試みます。");
				try{
					if(!targetItem.File.Directory.Exists){
						targetItem.File.Directory.Create();
						Log.AddWarning("ディレクトリ {0} がみつかりませんでした。新規にディレクトリを作成しました。", targetItem.File.Directory.FullName);
					}

					using(FileStream fs = targetItem.File.Open(FileMode.Create, FileAccess.Write, FileShare.None)){
						using(StreamWriter sw = new StreamWriter(fs, Project.Setting.HtmlEncodingObj)){
							sw.Write(result.Result);
							sw.Close();
						}
						fs.Close();
					}
					targetItem.File.Refresh();
					string s = string.Format("ファイル {0} に書き込みました。(サイズ: {1})", targetItem.FilePath, targetItem.File.Length);
					Log.AddInfo(s);
					result.Message = s;
				} catch(IOException e){
					result.AddError("ファイル {0} に書き込めません : {1}", targetItem.FilePath, e.Message);
					Log.AddError("ファイル {0} に書き込めません : {1}", targetItem.FilePath, e.Message);
					return result;
				} catch(UnauthorizedAccessException e){
					result.AddError("ファイル {0} に書き込む権限がありません : {1}", targetItem.FilePath, e.Message);
					Log.AddError("ファイル {0} に書き込む権限がありません : {1}", targetItem.FilePath, e.Message);
					return result;
				}
			}
			return result;
		}

// private メソッド

		// ファイルからデータを読み込む
		private static string LoadFile(FileInfo targetFile, Encoding enc){
			string result = null;

			using(FileStream fs = targetFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, enc)){
					result = sr.ReadToEnd();
					sr.Close();
				}
				fs.Close();
			}
			return result;
		}


		//meta charsetから文字符号化方式を判別する
		private Encoding DetectEncodingFromMetaCharset(string data){
			Match m = myMetaCharsetReg.Match(data);
			if(m.Success){
				Log.AddInfo("meta charsetを検出しました。 {0}", m.Value);
				string charsetValue = m.Groups[1].Value;
				try{
					Encoding result = Encoding.GetEncoding(charsetValue);
					Log.AddInfo("charset: {1} / Encoding: {0} ", charsetValue, result);
					return result;
				} catch(ArgumentException e){
					Log.AddError("charset: {0} に対応するEncodingが取得できませんでした: {1}", charsetValue, e.Message);
					return null;
				}
			}

			Log.AddInfo("meta charsetを検出できませんでした。");
			return null;
		}

		// テンプレート・エクスポート・プロパティをパース
		// プラグインから呼べるようにpublic
		public string GeneralParse(string data){
			bool commentDelete = false;
			if(Setting.TemplateCommentDelete == true) commentDelete = true;
			return GeneralParse(data, commentDelete);
		}

		private string GeneralParse(string data, bool commentDelete){
			// 無限ループチェック
			if(++myDepthCounter > Setting.DepthMax){
				throw new ParserException("テンプレート処理数が上限の" + Setting.DepthMax.ToString() + "に達したため、無限ループの疑いがあると判断して処理を中止しました。テンプレート処理数の上限を増やしたい場合は、設定の「DepthMax/テンプレート最大数」の値を増やしてください。");
			}

			MarkedData md = MarkedData.Parse(data);
			if(md == null) return data;

			// 空要素プロパティは常に削除
			if(md.EndMark == null) commentDelete = true;

			switch(md.MarkType){
			case MarkType.Export:
				return ExportParse(md, commentDelete);
			case MarkType.Template:
				return TemplateParse(md, commentDelete);
			case MarkType.Property:
				return PropertyParse(md, commentDelete);
			default:
				return data;
			}
		}


		// テンプレートのパース
		private string TemplateParse(MarkedData md, bool commentDelete){
			Log.AddInfo("{0} テンプレートを認知 : {1} / commentDelete = {2}", Project.CurrentItem, md.MarkName, commentDelete);
			string result = md.FrontData;
			result += InnerTemplateParse(md, md.MarkName, commentDelete);

			// 後ろを再帰処理
			Log.AddInfo("{0} テンプレートの後ろのデータを処理 : {1}", Project.CurrentItem, md.MarkName);
			result += GeneralParse(md.BackData, commentDelete);
			return result;
		}

		// テンプレートの中身を処理
		private string InnerTemplateParse(MarkedData md, string templateName, bool commentDelete){
			Type pluginType = null;
			string result = "";

			// プラグインを使える設定の場合、検索
			if(Project.Setting.PluginKind != PluginKind.None){
				if(Project.PluginAssembly != null){
					string nameSpace = Project.GetPluginNameSpace();
					// 名前の - は無視
					string pName = nameSpace + '.' + templateName.Replace("-", "");
					pluginType = Project.PluginAssembly.GetType(pName);
				}

				// プラグインが見つかれば処理
				if(pluginType != null && pluginType.IsSubclassOf(typeof(EcmPluginBase))){
					if(!commentDelete) result += md.StartMark;
					result += ProcessPlugin(pluginType, md);
					if(!commentDelete) result += md.EndMark;
					return result;
				}
			}

			// テンプレートを探す
			EcmTemplate template = GetTemplate(templateName);
			if(template.Exists){
				template.Backup();
				string templateResult = null;
				if(Setting.EnableRegexTemplate){
					if(string.IsNullOrEmpty(md.InnerData)){
						Log.AddInfo("{0} テンプレートファイルの正規表現が有効ですが、現在コンテンツのデータが無いため、テンプレートをそのまま使用します : {1}", Project.CurrentItem, templateName);
						templateResult = template.GetData();
					} else {
						Log.AddInfo("{0} テンプレートファイルの正規表現を検索 : {1}", Project.CurrentItem, templateName);
						TemplateRegexParser trp = new TemplateRegexParser(Project, Log, templateName);
						templateResult = trp.Parse(md, template.GetData());
					}
				} else {
					templateResult = template.GetData();
				}
				if(!commentDelete) result += md.StartMark;
				result += GeneralParse(templateResult);
				if(!commentDelete) result += md.EndMark;
				return result;
			}

			// CSV データでテンプレートが指定されていないか探す
			string csvTemplateName = Project.CurrentItem[templateName];
			if(!string.IsNullOrEmpty(csvTemplateName)){
				Log.AddInfo("{0} データ {1} でテンプレート {2} が指定されています。", Project.CurrentItem, templateName, csvTemplateName);
				return InnerTemplateParse(md, csvTemplateName, commentDelete);
			}

			Log.AddAlert("{0} テンプレートがみつかりませんでした。: {1}", Project.CurrentItem, templateName);

			// テンプレートデータがなければ中身を再帰処理
			result += md.StartMark;
			result += GeneralParse(md.InnerData);
			result += md.EndMark;
			return result;
		}



		// エクスポートのパース
		private string ExportParse(MarkedData md, bool commentDelete){
			Log.AddInfo("{0} エクスポートを認知 : {1}", Project.CurrentItem, md.MarkName);

			string result = md.FrontData + md.StartMark;
			// エクスポートを書き戻す
			string exportData = myExp[md.MarkName];
			if(exportData == null){
				Log.AddWarning("{0} エクスポートされた内容がありませんでした : {1}", Project.CurrentItem, md.MarkName);
				result += GeneralParse(md.InnerData, false);
			} else {
				Log.AddInfo("{0} エクスポート内容を処理 : {1}", Project.CurrentItem, md.MarkName);
				result += GeneralParse(exportData, false);
			}
			result += md.EndMark;

			// 後ろを再帰処理
			Log.AddInfo("{0} エクスポートの後ろのデータを処理 : {1}", Project.CurrentItem, md.MarkName);
			result += GeneralParse(md.BackData, commentDelete);
			return result;
		}


		// プロパティをパース
		private string PropertyParse(MarkedData md, bool commentDelete){
			Log.AddInfo("{0} プロパティを認知 : {1}", Project.CurrentItem, md.MarkName);

			string result = md.FrontData;
			if(!commentDelete) result += md.StartMark;

			// プロパティデータ取得ターゲットを取得
			EcmItem targetItem = Project.GetItem(md.PropertyIdName);

			// ターゲットが見つからなければ処理しない
			if(targetItem == null){
				Log.AddAlert("{0} にて ID : {1} のオブジェクトを要求しましたが、発見できませんでした。", Project.CurrentItem, md.PropertyIdName);
			} else {
				Object item = targetItem;
				Match ms = myMsReg.Match(md.PropertyParamName);
				while(ms.Success){
					string memberStr = ms.Groups[1].Value;
					Log.AddInfo("{0} メンバ {1} を認知", Project.CurrentItem, memberStr);
					if(item == null){
						Log.AddAlert("{0} {1}{2} にて {3} を呼び出そうとしましたが、オブジェクトが null です。", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName, memberStr);
					}
					try{
						item = Eval(item, memberStr);
					} catch(Exception e){
						throw new ParserException("{0} {1}{2} にて {3} を呼び出しましたが、例外が発生しました : {4}", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName, memberStr, e);
					}
					if(item == null){
						Log.AddWarning("{0} {1}{2} にて {3} を呼び出した結果は null になりました。", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName, memberStr);
						break;
					}
					ms = ms.NextMatch();
				}
				if(item == null){
					Log.AddWarning("{0} {1}{2} の結果は null になりました。", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName);
				} else {
					if(item is Array){
						foreach(Object o in item as Object[]){
							result += o.ToString();
						}
					} else {
						result += item.ToString();
					}
				}
			}

			if(!commentDelete) result += md.EndMark;

			// 後ろを再帰処理
			Log.AddInfo("{0} プロパティの後ろのデータを処理 : {1}", Project.CurrentItem, md.MarkName);
			result += GeneralParse(md.BackData, commentDelete);
			return result;
		}



		// テンプレートオブジェクトを取得する
		private EcmTemplate GetTemplate(string templateName){
			EcmTemplate result = new EcmTemplate(Project, templateName);
			return result;
		}



		// 擬似メソッドの実行
		// 結果 = EcmString or EcmString[]
		public Object Eval(Object item, string memberStr){
			if(item is Array){
				Log.AddInfo("{0} Eval処理: オブジェクトは {1} です。配列ですのでイテレータ処理を行います。", Project.CurrentItem, item);
				item = EvalArray(item as Object[], memberStr);
			} else if(item is EcmItem){
				Log.AddInfo("{0} Eval処理: オブジェクトは EcmItem {1} です。", Project.CurrentItem, item);
				item = EvalItem(item as EcmItem, memberStr);
			} else if(item is EcmString){
				Log.AddInfo("{0} Eval処理: オブジェクトは {1} {2} です。", Project.CurrentItem, item.GetType(), item);
				item = EvalString(item as EcmString, memberStr);
			} else {
				Log.AddInfo("{0} Eval処理: オブジェクトは 文字列(長さ:{1})です。", Project.CurrentItem, item.ToString().Length);
				item = EvalString(new EcmString(item.ToString(), Project), memberStr);
			}
			return item;
		}

		// EcmItem に対して Eval 処理を実行します。
		private Object EvalItem(EcmItem ei, string memberStr){
			if(memberStr == "") return ei;

			string[] members = memberStr.Split('(', ')');
			string memberName = members[0];
			string memberParam = null;

			// memberParam が null→プロパティ、""→引数なしメソッド、何かある→引数つきメソッド
			if(members.Length > 1){
				// メソッド
				Log.AddInfo("{0} メソッド {1} を認知", Project.CurrentItem, memberStr);
				MethodInfo m;
				memberParam = members[1];
				if(memberParam == ""){
					// 引数ナシメソッド
					Log.AddInfo("{0} メソッド {1} に引数は指定されていません", Project.CurrentItem, memberStr);
					m = typeof(EcmItem).GetMethod(memberName, Type.EmptyTypes);
					if(m != null) return m.Invoke(ei, null);
				} else {
					// 引数つきメソッド
					Log.AddInfo("{0} メソッド {1} の引数は {2} です", Project.CurrentItem, memberStr, memberParam);
					m = typeof(EcmItem).GetMethod(memberName, new Type[]{typeof(string)});
					if(m != null) return m.Invoke(ei, new Object[]{memberParam});
				}
				// みつからない
				Log.AddWarning("メソッドが見つかりませんでした : {0}.{1}", ei, memberStr);
			} else {
				// プロパティ
				Log.AddInfo("{0} プロパティ {1} を認知", Project.CurrentItem, memberStr);
				PropertyInfo p;

				// インデクサにあたる。
				if(ei[memberName] != null) return ei[memberName];

				// EcmItem のデフォルトプロパティにあたる
				p = typeof(EcmItem).GetProperty(memberName);
				if(p != null) return p.GetValue(ei, null);

				// Exportにあたる (その場で Parse する)
				string expTarget = ei.GetExport(this, memberStr);
				if(expTarget != null){
					Log.AddInfo("{0} の Export {1} を取得しました (サイズ : {2})", ei.FqId, memberStr, expTarget.Length);
					return GeneralParse(expTarget);
				}

				// みつからない
				Log.AddWarning("プロパティが見つかりませんでした : {0}.{1}", ei, memberStr);
				return null;
			}
			return ei;
		}

		// EcmString とその派生クラスに対して Eval 処理を実行します。
		private Object EvalString(EcmString o, string memberStr){
			if(memberStr == "") return o;

			Type t = o.GetType();

			string[] members = memberStr.Split('(', ')');
			string memberName = members[0];
			string memberParam = null;

			// memberParam が null→プロパティ、""→引数なしメソッド、何かある→引数つきメソッド
			if(members.Length > 1){
				// メソッド
				Log.AddInfo("{0} メソッド {1} を認知", Project.CurrentItem, memberStr);
				MethodInfo m;
				memberParam = members[1];
				if(memberParam == ""){
					// 引数ナシメソッド
					Log.AddInfo("{0} メソッド {1} に引数は指定されていません", Project.CurrentItem, memberStr);
					m = t.GetMethod(memberName, Type.EmptyTypes);
					if(m != null) return m.Invoke(o, null);
				} else {
					// 引数つきメソッド
					Log.AddInfo("{0} メソッド {1} の引数は {2} です", Project.CurrentItem, memberStr, memberParam);
					m = t.GetMethod(memberName, new Type[]{typeof(string)});
					if(m != null) return m.Invoke(o, new Object[]{memberParam});
				}
				// みつからない
				Log.AddWarning("メソッドが見つかりませんでした : {0}.{1}", o, memberStr);
			} else {
				// プロパティ
				Log.AddInfo("{0} プロパティ {1} を認知", Project.CurrentItem, memberStr);
				PropertyInfo p;

				p = t.GetProperty(memberName);
				if(p != null) return p.GetValue(o, null);

				// みつからない
				Log.AddWarning("プロパティが見つかりませんでした : {0}.{1}", o, memberStr);
			}
			return o;
		}

		private Object EvalArray(Object[] items, string memberStr){
			Log.AddInfo("{0} イテレータ処理の対象: {1}(要素数{2}) .{3}", Project.CurrentItem, items.GetType(), items.Length, memberStr);

			Object[] result = new Object[items.Length];
			for(int i = 0; i < items.Length; i++){
				Log.AddInfo("{0} イテレータ処理({1}/{2})", Project.CurrentItem, i+1, items.Length);
				Object o = Eval(items[i], memberStr);
				Log.AddInfo("{0} イテレータ処理{1}の結果: {2}", Project.CurrentItem, i+1, o);
				if(o is EcmItem){
					result[i] = o as EcmItem;
				} else {
					result[i] = new EcmString(o.ToString(), Project);
				}
			}

			return result;
		}




		// プラグイン処理
		// プラグインマネージャから呼ばれることがあるので public
		public string ProcessPlugin(Type pluginType, MarkedData md){
			Log.AddInfo("{0} プラグインを発見 : {1}", Project.CurrentItem, pluginType.Name);

			// このプラグインが呼ばれるのは何回目?
			if(myPluginProcessCounter.ContainsKey(pluginType)){
				myPluginProcessCounter[pluginType]++;
			} else {
				myPluginProcessCounter.Add(pluginType, 1);
			}
			int calledCount = myPluginProcessCounter[pluginType];

			string result = null;
			ConstructorInfo ci = null;
			EcmPluginBase pluginInstance = null;

			try{
				ci = pluginType.GetConstructor(EcmPluginBase.PluginConstractorParams);
				if(ci == null){
					Log.AddAlert("{0} コンストラクタが取得できませんでした。", Project.CurrentItem);
					return null;
				}
			} catch(Exception e) {
				Log.AddAlert("{0} コンストラクタの取得時に例外が発生しました。{1}", Project.CurrentItem, e);
				return null;
			}

			try{
				Object o = ci.Invoke(new Object[]{new EcmPluginParams(this, Setting, Project, md, calledCount)});
				pluginInstance = o as EcmPluginBase;
				if(pluginInstance == null){
					Log.AddAlert("{0} コンストラクタを実行しましたが、オブジェクトが作成されませんでした。", Project.CurrentItem);
					return null;
				}
			} catch(Exception e) {
				Log.AddAlert("{0} コンストラクタの実行中に例外が発生しました。{1}", Project.CurrentItem, e);
				return null;
			}

			try{
				pluginInstance.Parse();
				result = pluginInstance.Document.ToString();
			} catch(Exception e) {
				Log.AddAlert("{0} プラグインの実行中に例外が発生しました。{1}", Project.CurrentItem, e);
				return null;
			}

			Log.AddInfo("{0} プラグイン{1}の処理完了。結果の文字列長:{2}", Project.CurrentItem, pluginType.Name, result.Length);

			return result;
		}


	}


// Exception

	[Serializable()]
	public class ParserException : Exception{
		public ParserException(Object o) : base(o.ToString()){}
		public ParserException(string s, params Object[] o) : base(string.Format(s, o)){}
		public ParserException(string s) : base(s){}
		public ParserException(string s, Exception e) : base(s, e){}
	};

}




