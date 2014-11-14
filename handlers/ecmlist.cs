using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Bakera.Eccm{

	public class EcmList : EcmProjectHandler{

		public new const string PathName = null;
		public new const string Name = "�ꗗ";
		private readonly Regex whereRuleReg = new Regex("^([^<=>!]+)([<=>]{1,2}|!=)([^<=>!]+)$");

		public const string EccmPreviewName = "Preview";
		public const string EccmParseName = "Publish";
		public const string EccmSizeName = "Size";
		public const string EccmTimeName = "Time";

		private string mySortColumn;
		private string myWhereExpression;
		private bool myReverse;

		private const string PrivateSettingFlagName = "_privateSetting";
		private const string PathNameInputLabel = "_pathname";
		private const string PathNameCookieLabelPrefix = "EccmWorkingCopyPath";
		private const string LocalUriInputLabel = "_localuri";
		private const string LocalUriCookieLabelPrefix = "EccmLocalServerUri";

		private const string WhereInputLabel = "where";
		private const string SortInputLabel = "sort";
		private const string SortReverseInputLabel = "reverse";


// �R���X�g���N�^

		public EcmList(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}


// �I�[�o�[���C�h���\�b�h

		public override EcmResponse Get(HttpRequest rq){
			try{
				if(!File.Exists(Setting.CsvFullPath)) return ProjectNull(Project.Id);

				XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

				if(myProject.DataCount == 0){
					result.AppendChild(myXhtml.P(null, "�f�[�^���ݒ肳��Ă��܂���BCSV �f�[�^�̓��e���m�F���Ă��������B"));
				}




				// CSV �ւ̃����N
				if(!string.IsNullOrEmpty(myProject.Setting.CsvLinkPath)){
					string hrefValue = myProject.Setting.CsvLinkPath;
					if(hrefValue.StartsWith("\\\\")){
						hrefValue = "file:" + hrefValue.Replace('\\', '/');
					}
					XmlElement p = myXhtml.P("csvlink");
					p.InnerText = "CSV�t�@�C�� : ";
					XmlElement a = myXhtml.CreateElement("a");
					a.SetAttribute("href", hrefValue);
					a.InnerText = myProject.Setting.CsvLinkPath;
					p.AppendChild(a);
					result.AppendChild(p);
				}

				// �\�[�g�p�����[�^�擾
				// ���Ȃ݂� URL �f�R�[�h�ς݂ŕԂ��Ă���
				string sortStr = rq.Params[SortInputLabel];
				if(!string.IsNullOrEmpty(sortStr) && myProject.Columns[sortStr] != null){
					mySortColumn = sortStr;
				} else if(!string.IsNullOrEmpty(myProject.Setting.DefaultSortColumn)){
					mySortColumn = myProject.Setting.DefaultSortColumn;
				}
				if(rq.Params[SortReverseInputLabel] != null) myReverse = true;
				myWhereExpression = rq.Params[WhereInputLabel];
				string whereStr = whereParse(myWhereExpression);

				if(!String.IsNullOrEmpty(Setting.DisplayRowRule)){
					if(!String.IsNullOrEmpty(whereStr)){
						whereStr += " AND ";
					}
					whereStr += Setting.DisplayRowRule;
				}

				EcmItem[] items = null;
				try{
					items = myProject.GetAllItems(whereStr, mySortColumn, myReverse);
				} catch(EvaluateException) {
					items = myProject.GetAllItems(null, mySortColumn, myReverse);
				}

				// �x����\��
				result.AppendChild(ViewWarnings());

				if(items != null){
					result.AppendChild(whereForm(myProject.Rows.Count, items.Length));
					result.AppendChild(GetTable(items, rq));
				}

				// Cookie �ݒ�p�t�H�[��
				result.AppendChild(GetPersonalForm(rq));

				return new HtmlResponse(myXhtml, result);
			} catch(Exception e) {
				return ShowError("�V�X�e���G���[���������܂��� : {0}", e.ToString());
			}
		}

		public override EcmResponse Post(HttpRequest rq){
			if(Setting.CsvFullPath == null) return ProjectNull(myProject.Id);
			if(rq.Form.AllKeys.Length == 0){
				return ShowError("POST ����܂������A�p�����[�^�������Ă��܂���B");
			}

			if(rq.Form[PrivateSettingFlagName] != null){
				return Cookie(rq);
			}

			return Parse(rq);
		}



// �v���C�x�[�g���\�b�h

		// �x����\��
		private XmlNode ViewWarnings(){

			int errorCount = 0;
			XmlElement ul = myXhtml.Create("ul");

			string[] errorIds = Project.GetDuplicateId();
			if(errorIds.Length > 0){
				errorCount++;
				string mes = string.Format("ID: {0} ���d�����Ă��܂��B", string.Join(", ", errorIds));
				ul.AppendChild(myXhtml.Create("li", null, mes));
			}

			string[] errorPaths = Project.GetDuplicatePath();
			if(errorPaths.Length > 0){
				errorCount++;
				string mes = string.Format("�����̍��ڂœ���̃t�@�C�� \"{0}\" ���Q�Ƃ���Ă��܂��B", string.Join(", ", errorPaths));
				ul.AppendChild(myXhtml.Create("li", null, mes));
			}

			string[] miscErrors = Project.GetMiscErrors();
			foreach(string s in miscErrors){
				errorCount++;
				ul.AppendChild(myXhtml.Create("li", null, s));
			}

			if(errorCount > 0){
				XmlElement result = myXhtml.Create("div", "warnings");
				result.AppendChild(myXhtml.H(2, null, "�x��"));
				result.AppendChild(ul);
				return result;
			}
			return myXhtml.CreateDocumentFragment();
		}



		// where �����
		private string whereParse(string s){
			if(s ==null) return null;
			string result = "";
			foreach(string rule in s.Split(new char[]{' ', ','})){
				Match m = whereRuleReg.Match(rule);
				if(!m.Success) continue;
				if(m.Groups.Count < 4)  continue;

				string columnName = m.Groups[1].ToString();
				if(myProject.Columns[columnName] == null) continue;

				string op = m.Groups[2].ToString();
				if(op.Equals("==")) op = "=";
				if(op.Equals("=<")) op = "<=";
				if(op.Equals("=>")) op = ">=";
				if(op.Equals("><")) op = "<>";
				if(op.Equals("!=")) op = "<>";

				string dataValue = m.Groups[3].ToString();

				if(result != "") result += " AND ";
				result += string.Format("[{0}]{1}'{2}'", columnName, op, dataValue.Replace("\'", "''"));
			}
			return result;
		}


		// �i�荞�݃t�H�[��
		private XmlNode whereForm(int allCount, int currentCount){
			XmlElement form = myXhtml.Form(null, "get");
			XmlElement fieldset = myXhtml.Create("fieldset");
			XmlElement formP = myXhtml.Create("p");
			XmlNode input = myXhtml.Input(WhereInputLabel, myWhereExpression, "�i�荞�ݏ���");
			formP.AppendChild(input);
			XmlElement submit = myXhtml.CreateSubmit("�i�荞��");
			formP.AppendChild(submit);
			if(mySortColumn != null){
				XmlNode sortHidden = myXhtml.Hidden(SortInputLabel, mySortColumn);
				formP.AppendChild(sortHidden);
			}
			if(myReverse == true){
				XmlNode sortReverseHidden = myXhtml.Hidden(SortReverseInputLabel, "1");
				formP.AppendChild(sortReverseHidden);
			}

			form.AppendChild(formP);
			string countMessage = string.Format(" (�S {0} ���� {1} ����\��)", allCount, currentCount);
			formP.AppendChild(myXhtml.Text(countMessage));

			return form;
		}


		// �l�ݒ�t�H�[��
		private XmlNode GetPersonalForm(HttpRequest rq){

			string pathCookie = GetCookie(rq, PathNameCookieLabelPrefix);
			string uriCookie = GetCookie(rq, LocalUriCookieLabelPrefix);

			XmlElement form = myXhtml.Form(null, "post");
			XmlElement fieldset = myXhtml.Create("fieldset");
			XmlElement legend = myXhtml.Create("legend");
			legend.InnerText = "�l�ݒ�";

			XmlElement formP = myXhtml.Create("p");
			string wcHrefValue = pathCookie;
			formP.InnerText = "���[�L���O�R�s�[��URL : ";
			if(wcHrefValue != null){
				if(wcHrefValue.StartsWith("\\\\")){
					wcHrefValue = "file:" + wcHrefValue.Replace('\\', '/');
				}
				XmlElement formA = myXhtml.CreateElement("a");
				formA.SetAttribute("href", wcHrefValue);
				formA.InnerText = pathCookie;
				formP.AppendChild(formA);
				formP.AppendChild(myXhtml.CreateTextNode(" "));
			}
			XmlNode input = myXhtml.Input(PathNameInputLabel, pathCookie, null);
			formP.AppendChild(input);

			XmlElement formP2 = myXhtml.Create("p");
			string luHrefValue = uriCookie;
			formP2.InnerText = "���[�J������URL : ";
			if(luHrefValue != null){
				XmlElement formA = myXhtml.CreateElement("a");
				formA.SetAttribute("href", luHrefValue);
				formA.InnerText = uriCookie;
				formP2.AppendChild(formA);
				formP2.AppendChild(myXhtml.CreateTextNode(" "));
			}
			XmlNode input2 = myXhtml.Input(LocalUriInputLabel, uriCookie, null);
			formP2.AppendChild(input2);

			XmlElement submitP = myXhtml.P("submit");
			XmlElement submit = myXhtml.CreateSubmit("�ݒ�");
			submit.SetAttribute("name", PrivateSettingFlagName);
			submitP.AppendChild(submit);

			fieldset.AppendChild(legend);
			fieldset.AppendChild(formP);
			fieldset.AppendChild(formP2);
			fieldset.AppendChild(submit);
			form.AppendChild(fieldset);
			
			return form;
		}

		private string GetCookie(HttpRequest rq, string cookiePrefix){
			string cookieName = cookiePrefix + myProject.Id;
			HttpCookie targetCookie = rq.Cookies[cookieName];
			if(targetCookie == null) return null;
			return targetCookie.Value;
		}

		// �v���W�F�N�g�������|��\�����܂��B
		protected EcmResponse ProjectNull(string projectId){
			return ShowError("�v���W�F�N�g�� CSV�f�[�^�̃p�X [{0}] �Ƀt�@�C��������܂���BCSV �t�@�C�����쐬���邩�A�u�ݒ�v����p�X�̐ݒ���s���Ă��������B", Setting.CsvFullPath);
		}


		// ����̃A�C�e�����p�[�X���܂��B
		private EcmResponse Parse(HttpRequest rq){
			string targetId = rq.Form.AllKeys[0];
			EcmItem targetItem = myProject[targetId];
			if(targetItem == null) return ShowError("ID : {0} �̍��ڂ��擾�ł��܂���ł����B", targetId);

			myProject.Log.AddInfo("�p�[�X�Ώۍ��� : {0}", targetItem.Id);

			Parser p = new Parser(myProject);
			ProcessResult pr = p.Process(targetItem);

			// ���ʂ̕\��
			XmlDocumentFragment disp = Html.CreateDocumentFragment();

			if(pr.Errors.Length > 0){
				foreach(string s in pr.Errors){
					XmlElement mes = myXhtml.P();
					mes.InnerText = s;
					disp.AppendChild(mes);
				}
			} else {
				XmlElement mes = myXhtml.P();
				mes.InnerText = pr.Message;
				disp.AppendChild(mes);
			}


			XmlElement parseForm = myXhtml.Create("form");
			parseForm.SetAttribute("action", "");
			parseForm.SetAttribute("method", "post");

			XmlElement parseP = myXhtml.P();

			XmlElement returnLink = myXhtml.Create("a", null, "�ꗗ�ɖ߂�");
			returnLink.SetAttribute("href", myProject.Id);
			parseP.AppendChild(returnLink);

			parseP.AppendChild(myXhtml.Space());

			string previewUrl = myProject.Setting.PreviewRootUrl.TrimEnd('/') + targetItem.Path;
			XmlElement previewLink = myXhtml.Create("a", null, string.Format("{0}���m�F", targetItem.Id));
			previewLink.SetAttribute("href", previewUrl);
			parseP.AppendChild(previewLink);

			parseP.AppendChild(myXhtml.Space());


			string buttonLabel = string.Format("{0} ���ӂ����� {1}", targetId, EccmParseName);
			XmlElement parseButton = myXhtml.CreateSubmit(buttonLabel);
			parseButton.SetAttribute("name", targetId);

			parseP.AppendChild(parseButton);
			parseForm.AppendChild(parseP);
			disp.AppendChild(parseForm);


			// ���O�̕\��
			XmlElement h3 = myXhtml.H(3);
			h3.InnerText = "���O";
			disp.AppendChild(h3);
			EcmLogItem[] logs = p.Log.GetAll();

			Dictionary<EcmErrorLevel, int> errorCount = new Dictionary<EcmErrorLevel, int>();
			foreach(EcmErrorLevel eel in Enum.GetValues(typeof(EcmErrorLevel))){
				errorCount[eel] = 0;
			}

			if(logs.Length > 0){
				XmlElement logUl = myXhtml.Create("ol", EccmParseName + "Log");
				for(int i=0; i < logs.Length; i++){
					EcmLogItem eli = logs[i];
					errorCount[eli.Kind]++;
					XmlElement logLi = myXhtml.Create("li", eli.Kind.ToString().ToLower());
					XmlElement logStatus = myXhtml.Create("em", "status");
					logStatus.InnerText = "[" + eli.Kind.ToString() + "]";
					logLi.InnerText = eli.Data;
					logLi.PrependChild(logStatus);
					logLi.PrependChild(myXhtml.Text(eli.Time.ToString("yyyy-MM-dd hh:mm:ss ffff")));
					logUl.AppendChild(logLi);
				}

				XmlElement logCountUl = myXhtml.Create("ul", EccmParseName + "LogCount");
				foreach(EcmErrorLevel eel in Enum.GetValues(typeof(EcmErrorLevel))){
					if(errorCount[eel] == 0) continue;
					XmlElement logCountLi = myXhtml.Create("li", eel.ToString().ToLower());

					XmlElement logStatus = myXhtml.Create("em", "status");
					logStatus.InnerText = eel.ToString() + " : ";
					logCountLi.InnerText = errorCount[eel].ToString();
					logCountLi.PrependChild(logStatus);

					logCountUl.AppendChild(logCountLi);
				}
				disp.AppendChild(logCountUl);

				disp.AppendChild(logUl);
			}

			return new HtmlResponse(myXhtml, disp);
		}



		// Cookie �𔭍s���܂��B
		private EcmResponse Cookie(HttpRequest rq){

			XmlDocumentFragment doc = myXhtml.CreateDocumentFragment();
			HttpCookieCollection cookies = new HttpCookieCollection();

			string pathValue = rq.Form[PathNameInputLabel];
			if(!string.IsNullOrEmpty(pathValue)){
				XmlElement disp = myXhtml.Create("p");
				disp.InnerText = "���[�L���O�R�s�[�̃p�X��ݒ肵�܂��� : " + pathValue;
				doc.AppendChild(disp);

				HttpCookie pathCookie = new HttpCookie(PathNameCookieLabelPrefix + myProject.Id, pathValue);
				DateTime dt = DateTime.Now;
				TimeSpan ts = new TimeSpan(30, 0, 0, 0);
				pathCookie.Expires = dt.Add(ts);
				cookies.Add(pathCookie);
			}

			string localUriValue = rq.Form[LocalUriInputLabel];
			if(!string.IsNullOrEmpty(localUriValue)){
				XmlElement disp = myXhtml.Create("p");
				disp.InnerText = "���[�J������ Uri ��ݒ肵�܂��� : " + localUriValue;
				doc.AppendChild(disp);

				HttpCookie localUriCookie = new HttpCookie(LocalUriCookieLabelPrefix + myProject.Id, localUriValue);
				DateTime dt = DateTime.Now;
				TimeSpan ts = new TimeSpan(30, 0, 0, 0);
				localUriCookie.Expires = dt.Add(ts);
				cookies.Add(localUriCookie);
			}

			HtmlResponse result = new HtmlResponse(myXhtml, doc);
			result.Cookies = cookies;
			return result;
		}


// ========

		// ECMItem �̃��X�g���� HTML �� table ���쐬���܂��B
		private XmlNode GetTable(EcmItem[] items, HttpRequest rq){

			string pathCookie = GetCookie(rq, PathNameCookieLabelPrefix);
			string uriCookie = GetCookie(rq, LocalUriCookieLabelPrefix);

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
				XmlElement th = myXhtml.Create("th");
				XmlElement a = myXhtml.Create("a");
				a.InnerText = c.ToString();
				string hrefValue = string.Format("?{0}={1}", SortInputLabel, HttpUtility.UrlEncode(c.ColumnName));
				if(c.ColumnName == mySortColumn){
					XmlElement sortSpan = myXhtml.Create("span", "sort");
					if(myReverse){
						sortSpan.InnerText +="��";
					} else {
						sortSpan.InnerText +="��";
						hrefValue += string.Format("&{0}=1", SortReverseInputLabel);
					}
					a.AppendChild(sortSpan);
				}
				if(!string.IsNullOrEmpty(myWhereExpression)) hrefValue += '&' + WhereInputLabel + '=' + HttpUtility.UrlEncode(myWhereExpression);
				a.SetAttribute("href", hrefValue);
				th.AppendChild(a);
				if(c == myProject.IdCol && !string.IsNullOrEmpty(uriCookie)){
					th.SetAttribute("colspan", "2");
				}
				theadtr.AppendChild(th);
			}

			// �����
			if(!string.IsNullOrEmpty(myProject.Setting.ImageDir)){
				XmlElement previewTh = myXhtml.Create("th");
				previewTh.InnerText = EccmPreviewName;
				theadtr.AppendChild(previewTh);
			}

			XmlElement parseTh = myXhtml.Create("th");
			parseTh.InnerText = EccmParseName;
			theadtr.AppendChild(parseTh);

			XmlElement sizeTh = myXhtml.Create("th");
			sizeTh.InnerText = EccmSizeName;
			theadtr.AppendChild(sizeTh);

			XmlElement timeTh = myXhtml.Create("th");
			timeTh.InnerText = EccmTimeName;
			theadtr.AppendChild(timeTh);


			// �F�������K�\��
			ColorPattern sepPattern = null;
			ColorPattern colorPattern1 = null;
			ColorPattern colorPattern2 = null;
			ColorPattern colorPattern3 = null;
			try{
				sepPattern = ColorPattern.Parse(myProject.Setting.ColorSeparateRule, myProject.IdCol.ColumnName);
				colorPattern1 = ColorPattern.Parse(myProject.Setting.ColorRule1, myProject.IdCol.ColumnName);
				colorPattern2 = ColorPattern.Parse(myProject.Setting.ColorRule2, myProject.IdCol.ColumnName);
				colorPattern3 = ColorPattern.Parse(myProject.Setting.ColorRule3, myProject.IdCol.ColumnName);
			} catch (ArgumentException e) {
				myProject.Log.AddAlert("�F�������[���̐��K�\���ɃG���[������悤�ł��B���K�\���R���p�C���̃��b�Z�[�W : {1}", e.Message);
			}
			string prevIdFragment = null;
			string tempIdFragment = null;
			int tbodyCount = 1;
			XmlElement tbody = null;

			for(int i=0; i < items.Length; i++){
				EcmItem item = items[i];
				if(sepPattern != null){
					tempIdFragment = sepPattern.MatchValue(item);
				}
				if(tbody == null || tempIdFragment != prevIdFragment){
					tbody = myXhtml.Create("tbody");
					string classAttr = (++tbodyCount % 2 == 0) ? "even" : "odd";
					tbody.SetAttribute("class", classAttr);
					table.AppendChild(tbody);
					prevIdFragment = tempIdFragment;
				}
				XmlElement tr = ItemToTr(item, pathCookie, uriCookie);
				if(i % 2 == 0){
					tr.AddClass("even");
				} else {
					tr.AddClass("odd");
				}
				if(item.File == null || !item.File.Exists){
					if(i % 2 == 0){
						tr.AddClass("nonexist-even");
					} else {
						tr.AddClass("nonexist-odd");
					}
				}
				if(colorPattern1 != null && colorPattern1.IsMatch(item)) tr.AddClass("pattern1");
				if(colorPattern2 != null && colorPattern2.IsMatch(item)) tr.AddClass("pattern2");
				if(colorPattern3 != null && colorPattern3.IsMatch(item)) tr.AddClass("pattern3");
				tbody.AppendChild(tr);
			}
			form.AppendChild(table);
			return form;
		}


		// EcmItem ����\�̗���擾���܂��B
		private XmlElement ItemToTr(EcmItem item, string pathCookie, string uriCookie){
			XmlElement tr = myXhtml.Create("tr");
			foreach(DataColumn c in myProject.Columns){
				if(IsHiddenColumn(c.ColumnName)) continue;
				XmlElement td = myXhtml.Create("td");
				if(c == myProject.IdCol && myProject.Setting.PreviewRootUrl != null && item.File != null && item.File.Exists){
					string previewUrl = myProject.Setting.PreviewRootUrl.TrimEnd('/') + item.Path;
					XmlElement a = myXhtml.Create("a");
					a.SetAttribute("href", previewUrl);
					a.InnerText = item.DataRow[c].ToString();
					td.AppendChild(a);
				} else if(c == myProject.PathCol && pathCookie != null && item.File != null && item.File.Exists){
					string workingCopyUrl = pathCookie.TrimEnd('/', '\\') + item.Path;
					XmlElement a = myXhtml.Create("a");
					a.SetAttribute("href", workingCopyUrl);
					a.InnerText = item.DataRow[c].ToString();
					td.AppendChild(a);
				} else {
					td.InnerText = item.DataRow[c].ToString();
				}
				tr.AppendChild(td);
				if(c == myProject.IdCol && !string.IsNullOrEmpty(uriCookie)){
					XmlElement localLinkTd = myXhtml.Create("td");
					if(item.File != null && item.File.Exists){
						string localPreviewUrl = uriCookie.TrimEnd('/') + item.Path;
						XmlElement localA = myXhtml.Create("a");
						localA.SetAttribute("href", localPreviewUrl);
						localA.SetAttribute("class", "localPreviewUrl");
						localA.InnerText = "[local]";
						localA.SetAttribute("title", item.DataRow[c].ToString());
						localLinkTd.AppendChild(localA);
					}
					tr.AppendChild(localLinkTd);
				}
			}

			// �v���r���[�摜
			if(!string.IsNullOrEmpty(myProject.Setting.ImageDir)){
				XmlElement previewTd = myXhtml.Create("td");
				if(item.PreviewFiles != null && item.PreviewFiles.Length > 0){
					XmlElement previewUl = myXhtml.Create("ul");
					for(int i=0; i < item.PreviewFiles.Length; i++){
						string previewHref = myProject.Id + "/" + PreviewManager.PathName + "/" + item.Id + "/" + (i+1).ToString();
						string previewDateStr = string.Format("�X�V��: {0}", item.PreviewFiles[i].LastWriteTime);

						XmlElement previewA = myXhtml.Create("a");
						previewA.SetAttribute("href", previewHref);
						previewA.SetAttribute("title", previewDateStr);
						previewA.InnerText = item.PreviewFiles[i].Name;
						XmlElement previewLi = myXhtml.Create("li");
						previewLi.AppendChild(previewA);
						previewUl.AppendChild(previewLi);
					}
					previewTd.AppendChild(previewUl);
				}
				tr.AppendChild(previewTd);
			}

			XmlElement parseTd = myXhtml.Create("td");
			if(item.ParsePermit && !string.IsNullOrEmpty(item.Path)){
				XmlElement parseButton = myXhtml.CreateSubmit(EccmParseName);
				parseButton.SetAttribute("name", item.Id);
				parseTd.AppendChild(parseButton);
				parseTd.SetAttribute("class", EccmParseName);
			}
			tr.AppendChild(parseTd);

			XmlElement sizeTd = myXhtml.Create("td");
			sizeTd.InnerText = item.FileSizeShort;
			tr.AppendChild(sizeTd);

			XmlElement timeTd = myXhtml.Create("td");
			timeTd.InnerText = item.FileTime;
			tr.AppendChild(timeTd);

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



