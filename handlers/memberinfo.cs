using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{


	// 特定プロジェクトの設定を表示します。
	public class MemberList : EcmProjectHandler{

		public MemberList(EcmProject proj, Xhtml xhtml) : base(proj, xhtml){}

		public new const string Name = "EcmItemのメンバ一覧";
		public new const string PathName = "memberlist";

		public static Type myEcmItemType = typeof(EcmItem);

// プロパティ
		public override string SubTitle{
			get{return Name;}
		}


// メソッド
		// 特定プロジェクトの設定を表示します。
		public override EcmResponse Get(HttpRequest rq){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();

			result.AppendChild(myXhtml.H(2, null, "EcmItemのプロパティ"));
			result.AppendChild(GetMemberList(myEcmItemType.GetProperties()));

			result.AppendChild(myXhtml.H(2, null, "EcmItemのメソッド"));
			result.AppendChild(GetMemberList(myEcmItemType.GetMethods()));

			return new HtmlResponse(myXhtml, result);
		}

		private XmlNode GetMemberList(MemberInfo[] members){
			XmlDocumentFragment result = myXhtml.CreateDocumentFragment();
			if(members.Length > 0){
				XmlElement ul = myXhtml.Create("ul");
				result.AppendChild(ul);
				foreach(MemberInfo m in members){
					ul.AppendChild(myXhtml.Create("li", null, m.Name));
				}
			}
			return result;
		}

	}
}


