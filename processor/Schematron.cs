using System;
using System.IO;
using System.Xml;
using NMatrix.Schematron;

namespace Bakera.Eccm{
	public class Schematron : EcmProcessorBase{

		public new const string Name = "SchematronによるXMLの検証";
		public new const string Description = "コンテンツを XML Schema / Schmatron で検証します。";


// コンストラクタ
		public Schematron(EcmProject proj) : base(proj){}

// public メソッド

		public override ProcessResult Process(EcmItem targetItem){
			ProcessResult result = new ProcessResult();
			if(!targetItem.File.Exists){
				result.Message = "ファイルがありません。";
				return result;
			}
			try{
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.ProhibitDtd = false;
				settings.XmlResolver = null;
				using(FileStream fs = targetItem.File.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
					using(StreamReader sr = new StreamReader(fs, Setting.HtmlEncodingObj)){
						XmlReader r = XmlReader.Create(sr, settings);

						if(!string.IsNullOrEmpty(targetItem.SchemaName)){
							if(targetItem.SchemaFile == null || !targetItem.SchemaFile.Exists){
								result.AddError("スキーマ {0} のファイルが見つかりません", targetItem.SchemaName);
								result.Message = "Error";
								return result;
							}
							Validator validator = new Validator(OutputFormatting.XML);
							validator.AddSchema(targetItem.SchemaFile.FullName);
							validator.Validate(r);
						}
						result.Message = "OK";
						sr.Close();
					}
					fs.Close();
				}
			} catch (XmlException e){
				result.Message = "XML エラー";
				result.AddError(e.Message);
			} catch (NMatrix.Schematron.ValidationException e){
				XmlDocument resultDoc = new XmlDocument();
				resultDoc.XmlResolver = null;
				resultDoc.LoadXml(e.Message);
				XmlNodeList patternList = resultDoc.GetElementsByTagName("pattern");
				if(patternList.Count > 0){
					result.Message = "Schematron検証エラー:";
					foreach(XmlElement p in patternList){
						string name = p.GetAttribute("name");
						string id = p.GetAttribute("id");

						XmlElement rule = p["rule"];
						string context = rule.GetAttribute("context");

						XmlElement message = rule["message"];
						XmlElement text = message["text"];
						XmlElement path = message["path"];
						XmlElement position = message["position"];

						string line = position.GetAttribute("line");
						string column = position.GetAttribute("column");
						result.AddError("{0}:{1}:{2}({3}行{4}列、{5})", id, name, text.InnerText, line, column, path.InnerText);
					}
				}
			}
			return result;
		}

	}


}




