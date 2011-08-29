using System;
using System.Web;

namespace Bakera.Eccm{
	public abstract class EcmPluginBase{

// フィールド
		protected readonly EcmPluginParams myParam;
		protected readonly EcmPluginDocument myDocument = new EcmPluginDocument();
		public static Type[] PluginConstractorParams = new Type[]{typeof(EcmPluginParams)};

// コンストラクタ
		// Setting と EcmProject, EcmItem を指定して、EcmPlugin クラスのインスタンスを作成します。
		protected EcmPluginBase(EcmPluginParams param){
			myParam = param;
		}


// プロパティ

		protected EcmItem Item{
			get{return Project.CurrentItem;}
		}

		protected Parser Parser{
			get{return myParam.Parser;}
		}

		protected Setting Setting{
			get{return myParam.Setting;}
		}

		protected EcmProject Project{
			get{return myParam.Project;}
		}

		public EcmPluginDocument Document{
			get{return myDocument;}
		}

		public MarkedData MarkedData{
			get{return myParam.MarkedData;}
		}

		public int CalledCount{
			get{return myParam.CalledCount;}
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

