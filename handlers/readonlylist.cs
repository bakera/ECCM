using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Bakera.Eccm{

	public class ReadOnlyList : EcmProjectHandler{

		public new const string PathName = "list";
		public new const string Name = "�ꗗ(�\���̂�)";

		private string mySortColumn;
		private bool myReverse;

		private const string PrivateSettingFlagName = "_privateSetting";
		private const string PathNameInputLabel = "_pathname";
		private const string PathNameCookieLabelPrefix = "EccmWorkingCopyPath";
		private const string LocalUriInputLabel = "_localuri";
		private const string LocalUriCookieLabelPrefix = "EccmLocalServerUri";

// �R���X�g���N�^

		public ReadOnlyList(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}


// �I�[�o�[���C�h���\�b�h

		public override EcmResponse Get(HttpRequest rq){
			if(!File.Exists(Setting.CsvFullPath)) return ProjectNull(Project.Id);
			if(myProject.DataCount == 0) return ShowError("�f�[�^������܂���BCSV �t�@�C���̓��e���m�F���Ă��������B");

			// �\�[�g�p�����[�^�擾
			// ���Ȃ݂� URL �f�R�[�h�ς݂ŕԂ��Ă���
			string sortStr = rq.Params["sort"];
			if(!string.IsNullOrEmpty(sortStr) && myProject.Columns[sortStr] != null){
				mySortColumn = sortStr;
			} else if(!string.IsNullOrEmpty(myProject.Setting.DefaultSortColumn)){
				mySortColumn = myProject.Setting.DefaultSortColumn;
			}
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			if(mySortColumn == null){
				result.AppendChild(GetTable(myProject.GetAllItems(), rq));
			} else {
				if(rq.Params["reverse"] != null) myReverse = true;
				EcmItem[] items = myProject.GetAllItems(mySortColumn, myReverse);
				result.AppendChild(GetTable(items, rq));
			}

			return new HtmlResponse(myXhtml, result);
		}

		public override EcmResponse Post(HttpRequest rq){
			return Get(rq);
		}

// �v���C�x�[�g���\�b�h

		// �v���W�F�N�g�������|��\�����܂��B
		protected EcmResponse ProjectNull(string projectId){
			return ShowError("�v���W�F�N�g�� CSV�f�[�^�̃p�X [{0}] �Ƀt�@�C��������܂���BCSV �t�@�C�����쐬���邩�A�u�ݒ�v����p�X�̐ݒ���s���Ă��������B", Setting.CsvFullPath);
		}

// ========

		// ECMItem �̃��X�g���� HTML �� table ���쐬���܂��B
		private XmlNode GetTable(EcmItem[] items, HttpRequest rq){

			XmlElement form = myXhtml.Create("form");
			form.SetAttribute("action", "");
			form.SetAttribute("method", "post");
			XmlElement table = myXhtml.Create("table");

			// thead
			XmlElement thead = myXhtml.Create("thead");
			table.AppendChild(thead);
			XmlElement theadtr = myXhtml.Create("tr");
			thead.AppendChild(theadtr);
			foreach(DataColumn c in myProject.Columns){
				if(IsHiddenColumn(c.ColumnName)) continue;
				if(c == myProject.PathCol) continue;
				XmlElement th = myXhtml.Create("th");
				th.InnerText = c.ToString();
				theadtr.AppendChild(th);
			}

			// �F�������K�\��
			Regex idReg = null;
			string colorSeparateTargetColumn = null;
			if(!string.IsNullOrEmpty(myProject.Setting.ColorSeparateRule)){
				string[] colorSeparateRules = myProject.Setting.ColorSeparateRule.Split('=');
				string colorSeparateRegexStr = null;
				if(colorSeparateRules.Length == 1){
					colorSeparateTargetColumn = myProject.IdCol.ColumnName;
					colorSeparateRegexStr = colorSeparateRules[0];
				} else {
					colorSeparateTargetColumn = colorSeparateRules[0];
					colorSeparateRegexStr = colorSeparateRules[1];
				}
				try{
					idReg = new Regex(colorSeparateRegexStr);
				} catch (ArgumentException e) {
					myProject.Log.AddAlert("�F�������[���̐��K�\���ɃG���[������悤�ł��B���K�\���R���p�C���̃��b�Z�[�W : {1}", e.Message);
				}
			}
			string prevIdFragment = null;
			string tempIdFragment = null;
			int tbodyCount = 1;
			XmlElement tbody = null;

			for(int i=0; i < items.Length; i++){
				EcmItem item = items[i];
				if(item.File == null || !item.File.Exists) continue;
				if(idReg != null){
					Match m = idReg.Match(item[colorSeparateTargetColumn]);
					if(m.Groups.Count > 1) tempIdFragment = m.Groups[1].Value;
				}
				if(tbody == null || tempIdFragment != prevIdFragment){
					tbody = myXhtml.Create("tbody");
					string classAttr = (++tbodyCount % 2 == 0) ? "even" : "odd";
					tbody.SetAttribute("class", classAttr);
					table.AppendChild(tbody);
					prevIdFragment = tempIdFragment;
				}
				XmlElement tr = ItemToTr(item);
				if(i % 2 == 0){
					tr.SetAttribute("class", "even");
				} else {
					tr.SetAttribute("class", "odd");
				}
				tbody.AppendChild(tr);
			}
			form.AppendChild(table);
			return form;
		}


		// EcmItem ����\�̗���擾���܂��B
		private XmlElement ItemToTr(EcmItem item){
			XmlElement tr = myXhtml.Create("tr");
			foreach(DataColumn c in myProject.Columns){
				if(IsHiddenColumn(c.ColumnName)) continue;
				if(c == myProject.PathCol) continue;
				XmlElement td = myXhtml.Create("td");
				if(c == myProject.IdCol && myProject.Setting.PreviewRootUrl != null && item.File != null && item.File.Exists){
					string previewUrl = myProject.Setting.PreviewRootUrl.TrimEnd('/') + item.Path;
					XmlElement a = myXhtml.Create("a");
					a.SetAttribute("href", previewUrl);
					a.InnerText = item.DataRow[c].ToString();
					td.AppendChild(a);
				} else {
					td.InnerText = item.DataRow[c].ToString();
				}
				tr.AppendChild(td);
			}
			return tr;
		}

		private bool IsHiddenColumn(string columnName){
			foreach(string s in myProject.Setting.HiddenColumnList){
				if(columnName.Equals(s, StringComparison.InvariantCultureIgnoreCase)) return true;
			}
			return false;
		}

	}

}



