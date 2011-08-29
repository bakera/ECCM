using System;
using System.Web;

namespace Bakera.Eccm{
	public abstract class EcmToolBase{

// フィールド
		protected readonly EcmPluginParams myParam;
		public static Type[] PluginConstractorParams = new Type[]{typeof(EcmPluginParams)};

// コンストラクタ
		// Setting と EcmProject, EcmItem を指定して、EcmPlugin クラスのインスタンスを作成します。
		protected EcmToolBase(EcmPluginParams param){
			myParam = param;
		}


// プロパティ

		protected EcmItem Item{
			get{return Project.CurrentItem;}
		}

		protected Setting Setting{
			get{return myParam.Setting;}
		}

		protected EcmProject Project{
			get{return myParam.Project;}
		}



// 抽象メソッド

		public abstract void Parse();

// 静的メソッド

		// 文字列を「HTML エンコード」します。
		public static string HtmlEncode(object s){
			return HttpUtility.HtmlEncode(s.ToString());
		}

		// 文字列を「HTML エンコード」します。
		public static string H(object s){
			return HtmlEncode(s);
		}

		// 文字列を「HTML エンコード」した上で引用符で括ります。
		public static string Q(object s){
			return QuoteAttribute(s);
		}

		// 文字列を「HTML エンコード」した上で引用符で括ります。
		public static string QuoteAttribute(object s){
			return '"' + HttpUtility.HtmlEncode(s.ToString()) + '"';
		}


	}
}

