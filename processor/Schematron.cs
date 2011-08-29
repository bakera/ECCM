using System;
using System.IO;
using System.Xml;
using NMatrix.Schematron;

namespace Bakera.Eccm{
	public class Schematron : EcmProcessorBase{

		public new const string Name = "Schematron�ɂ��XML�̌���";
		public new const string Description = "�R���e���c�� XML Schema / Schmatron �Ō��؂��܂��B";


// �R���X�g���N�^
		public Schematron(EcmProject proj) : base(proj){}

// public ���\�b�h

		public override ProcessResult Process(EcmItem targetItem){
			ProcessResult result = new ProcessResult();
			if(!targetItem.File.Exists){
				result.Message = "�t�@�C��������܂���B";
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
								result.AddError("�X�L�[�} {0} �̃t�@�C����������܂���", targetItem.SchemaName);
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
				result.Message = "XML �G���[";
				result.AddError(e.Message);
			} catch (NMatrix.Schematron.ValidationException e){
				XmlDocument resultDoc = new XmlDocument();
				resultDoc.XmlResolver = null;
				resultDoc.LoadXml(e.Message);
				XmlNodeList patternList = resultDoc.GetElementsByTagName("pattern");
				if(patternList.Count > 0){
					result.Message = "Schematron���؃G���[:";
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
						result.AddError("{0}:{1}:{2}({3}�s{4}��A{5})", id, name, text.InnerText, line, column, path.InnerText);
					}
				}
			}
			return result;
		}

	}


}




