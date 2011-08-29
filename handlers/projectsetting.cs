using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{


	// ����v���W�F�N�g�̐ݒ��\�����܂��B
	public class ProjectSetting : EcmProjectHandler{

		public ProjectSetting(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "�ݒ�";
		public new const string PathName = "setting";


// �v���p�e�B
		public override string SubTitle{
			get{return Name;}
		}


// ���\�b�h
		// ����v���W�F�N�g�̐ݒ��\�����܂��B
		public override EcmResponse Get(HttpRequest rq){
			XmlElement result = myXhtml.Create("form");
			result.SetAttribute("action", "");
			result.SetAttribute("method", "post");

			result.AppendChild(GetControls("��ʐݒ�", EccmFieldGenreType.General));
			result.AppendChild(GetControls("�\���ݒ�", EccmFieldGenreType.View));
			result.AppendChild(GetControls("�f�B���N�g���̐ݒ�", EccmFieldGenreType.Directory));
			result.AppendChild(GetControls("�����N�̐ݒ�", EccmFieldGenreType.Link));
			result.AppendChild(GetControls("�p�[�T�̐ݒ�", EccmFieldGenreType.Parser));
			result.AppendChild(GetControls("���̑��̐ݒ�", EccmFieldGenreType.Misc));

			XmlElement btn = myXhtml.CreateSubmit("�ݒ��ۑ�");
			XmlElement p = myXhtml.P();
			p.AppendChild(btn);
			result.AppendChild(p);
			
			return new HtmlResponse(myXhtml, result);
		}

		// �W���������ƂɃR���g���[�����擾���܂��B
		private XmlNode GetControls(string genreName, EccmFieldGenreType genre){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			Type t = myProject.Setting.GetType();
			PropertyInfo[] fields = t.GetProperties();
			XmlElement ul = myXhtml.Create("ul");

			int propCount = 0;
			foreach(PropertyInfo pi in fields){
				EccmDescriptionAttribute descAttr = null;
				EccmEditableAttribute editAttr = null;
				EccmFieldGenreAttribute genreAttr = null;

				Object[] attrs = pi.GetCustomAttributes(false);
				foreach(Object o in attrs){
					if(o is EccmDescriptionAttribute){
						descAttr = o as EccmDescriptionAttribute;
					} else if(o is EccmEditableAttribute){
						editAttr = o as EccmEditableAttribute;
					} else if(o is EccmFieldGenreAttribute){
						genreAttr = o as EccmFieldGenreAttribute;
					}
				}
				if(descAttr == null || genreAttr == null || genreAttr.Genre != genre) continue;
				ul.AppendChild(GetControl(pi, descAttr, editAttr));
				propCount++;
			}
			if(propCount == 0) return result;

			XmlElement h = myXhtml.H(2, null, genreName);
			result.AppendChild(h);
			result.AppendChild(ul);
			return result;
		}


		// �v���W�F�N�g�̐ݒ�𔽉f���܂��B
		public override EcmResponse Post(HttpRequest rq){

			// �V���� Setting �C���X�^���X���쐬

			Setting newSetting = new Setting();
			Type t = newSetting.GetType();
			PropertyInfo[] fields = t.GetProperties();
			foreach(PropertyInfo pi in fields){

				//XmlIgnore ��?
				bool ignore = false;
				Object[] attrs = pi.GetCustomAttributes(false);
				foreach(Object o in attrs){
					if(o is XmlIgnoreAttribute){
						ignore = true;
						break;
					}
				}
				if(ignore) continue;

				string propValue = rq.Form[pi.Name];
				Type propType = pi.PropertyType;
				Object result = null;
				if(propType == typeof(int)){
					int intResult = 0;
					int.TryParse(propValue, out intResult);
					result = intResult;
				} else if(propType == typeof(bool)){
					if(propValue != null){
						result = true;
					} else {
						result = false;
					}
				} else if(propType.IsEnum){
					try{
						result = Enum.Parse(propType, propValue);
					} catch {
						result = 0;
					}
				} else if(propType == typeof(string)){
					result = propValue;
				}
				pi.SetValue(newSetting, result, null);
			}
			

			XmlSerializer xs = new XmlSerializer(typeof(Setting));
			
			using(FileStream fs = myProject.Setting.BaseFile.Open(FileMode.Create, FileAccess.Write, FileShare.None)){
				using(StreamWriter sw = new StreamWriter(fs)){
					xs.Serialize(fs, newSetting);
					sw.Close();
				}
				fs.Close();
			}

			XmlElement p = myXhtml.P();
			p.InnerText = "�ݒ��ۑ����܂����B";
			return new HtmlResponse(myXhtml, p);
		}




		// �t�B�[���h�̒l��ݒ�E�擾���邽�߂̗v�f���擾���܂��B
		private XmlNode GetControl(PropertyInfo pi, EccmDescriptionAttribute desc, EccmEditableAttribute edit){
			if(desc == null) return myXhtml.CreateDocumentFragment();

			XmlNode result = myXhtml.Create("li");
			string propName = string.Format("{0} / {1}", pi.Name, desc.Name);
			XmlNode h = myXhtml.H(3, null, propName);

			XmlNode d = myXhtml.P("description");
			d.InnerText = desc.Description;
			result.AppendChild(h);
			result.AppendChild(d);


			Object piValueObj = pi.GetValue(myProject.Setting, null);
			string piValue = null;
			if(piValueObj != null) piValue = piValueObj.ToString();

			if(edit == null){
				XmlNode input = myXhtml.Hidden(pi.Name, piValue);
				XmlNode ed = myXhtml.P("no-edit", piValue, input);
				result.AppendChild(ed);
				return result;
			}

			Type pType = pi.PropertyType;
			
			if(pType == typeof(bool)){
				bool boxChecked = false;
				if(piValue != null && piValue.Equals("true", StringComparison.InvariantCultureIgnoreCase)) boxChecked = true;
				XmlNode input = myXhtml.Checkbox(pi.Name, "true", boxChecked, edit.Label);
				XmlNode ed = myXhtml.P("edit", input);
				result.AppendChild(ed);
			} else if(pType.IsEnum) {
				XmlElement select = myXhtml.Create("select");
				select.SetAttribute("name", pi.Name);
				select.SetAttribute("id", pi.Name);
				foreach(string n in Enum.GetNames(pType)){
					XmlElement option = myXhtml.Create("option");
					option.InnerText = n;
					if(n.Equals(piValue)) option.SetAttribute("selected", "selected");
					select.AppendChild(option);
				}

				XmlElement label = myXhtml.Create("label");
				label.SetAttribute("for", pi.Name);
				label.InnerText = desc.Name;
				XmlNode ed = myXhtml.P("edit", label, " : ", select);
				result.AppendChild(ed);
			} else {
				XmlNode input = myXhtml.Input(pi.Name, piValue, desc.Name);
				XmlNode ed = myXhtml.P("edit", input);
				result.AppendChild(ed);
			}


			return result;
		}


	}
}


