using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{


	// プレビュー画像の一覧を表示します。
	public class PreviewManager : EcmList{

		public PreviewManager(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "プレビュー画像";
		public new const string PathName = "preview";


// プロパティ
		public override string SubTitle{
			get{return Name;}
		}


// メソッド

		// 引数がないとき : 特定プロジェクトのプレビュー画像一覧を表示します。
		// ID が渡されたとき : その ID のプレビュー画像を返します。
		public override EcmResponse Get(HttpRequest rq){
			if(!File.Exists(Setting.CsvFullPath)) return ProjectNull(Project.Id);
			if(myProject.DataCount == 0) return ShowError("データがありません。CSV ファイルの内容を確認してください。");

			string[] imgIds = GetOptions(rq);
			if(imgIds.Length == 2){
				int imgNumber = 0;
				if(Int32.TryParse(imgIds[1], out imgNumber) && imgNumber > 0) return GetImage(imgIds[0], imgNumber);
			}

			// 画像一覧
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			EcmItem[] items = myProject.GetAllItems();

			int itemCount = 0;
			XmlElement ul = myXhtml.Create("ul");
			foreach(EcmItem item in items){
				if(item.PreviewFiles != null && item.PreviewFiles.Length > 0){
					XmlElement li = myXhtml.Create("li");
					li.InnerText = item.Id;
					XmlElement innerUl = myXhtml.Create("ul");
					for(int i=0; i < item.PreviewFiles.Length; i++){
						string previewHref = PreviewManager.PathName + "/" + item.Id + "/" + (i+1).ToString();
						XmlElement previewA = myXhtml.Create("a");
						previewA.SetAttribute("href", previewHref);
						previewA.InnerText = item.PreviewFiles[i].Name;
						XmlElement previewLi = myXhtml.Create("li");
						previewLi.AppendChild(previewA);
						innerUl.AppendChild(previewLi);
						itemCount++;
					}
					li.AppendChild(innerUl);
					ul.AppendChild(li);
				}
			}
			if(itemCount > 0){
				result.AppendChild(ul);
				return new HtmlResponse(myXhtml, result);
			}
			return ShowError("画像は登録されていません。");
		}

		private EcmResponse GetImage(string id, int num){
			EcmItem targetItem = myProject.GetItem(id);
			if(targetItem.PreviewFiles != null && targetItem.PreviewFiles.Length >= num){
				return new ImageResponse(targetItem.PreviewFiles[num-1]);
			}
			return ShowError("id: {0} の画像 {1} はみつかりませんでした。", id, num.ToString());
		}


		// POST には対応しません (GET と同じ)。
		public override EcmResponse Post(HttpRequest rq){
			return Get(rq);
		}


		private XmlNode GetTable(EcmItem[] items){
			return null;
		}

		protected string[] GetOptions(HttpRequest rq){
			string thisPath = "/" + myProject.Id + "/" + PathName + "/";
			string optionId = Util.CutLeft(rq.PathInfo, thisPath);
			string[] optionIds = optionId.Split('/');
			return optionIds;
		}


	}
}


