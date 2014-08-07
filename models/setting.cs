using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{

	[XmlRoot(Namespace = "", ElementName = "eccm-setting")]
	public class Setting{
		private XmlDocument myXml = null; // データソースの XML
		private string myId = null;
		private DateTime myStartTime;
		private FileInfo myBaseFile;
		private string myProjectName = "プロジェクト名未定";
		private string myRootDir = "c:\\";
		private string myDocumentDir = "document_root";
		private string myTemplateDir = "template";
		private string mySchemaDir = "schema";
		private string myCsvPath = "data.csv";
		private string myCsvLinkPath = "";
		private string myTemplateExt = ".txt";
		private string myPreviewRootUrl = "http://127.0.0.1"; // ルートとなる URL、末尾の / は不要
		private int myDepthMax = 100; // 無限ループ検出用のカウンタ上限
		private string myAnchorTemplate = "<a href=\"{0}\">{1}</a>";
		private string myStayTemplate = "<em class=\"stay\">{0}</em>";
		private bool myTemplateCommentDelete = true; // テンプレート内のECMコメントを削除するかどうか。trueは削除する
		private bool myAutoPathGenerate = false; // パス指定が無いときに ID からファイル名を自動生成するかどうか。false は生成しない
		private bool myEnableRegexTemplate = false;
		private string myCsvEncoding = "Shift_JIS"; // CSV の文字符号化方式
		private string myHtmlEncoding = "Shift_JIS"; // HTML の文字符号化方式
		private bool myHtmlEncodingDetectFromMetaCharset = false; // HTML の文字符号化方式
		private string myColorSeparateRule;
		private string myParsePermissonRule;
		private string mySvnUpdateCommand;
		private string myHandler = "ProjectSetting,AllParse";
		private string myImageDir;
		private string myHiddenColumn;
		private string[] myHiddenColumnList;
		private string myDefaultSortColumn;
		private string myDefaultGrobalTemplate;
		private string myDefaultSchemaName;
		private string myIndexLinkSuffix;
		private PluginKind myPluginKind;

		private static Assembly myAssembly;
		private static Type[] myHandlerTypes;
		private static Type[] myProcessorTypes;


// 静的初期化コンストラクタ
		static Setting(){
			myAssembly = Assembly.GetAssembly(typeof(Eccm));
			Type[] types = myAssembly.GetTypes();

			List<Type> handlerTypes = new List<Type>();
			List<Type> processorTypes = new List<Type>();

			foreach(Type t in types){
				if(t.IsSubclassOf(typeof(EcmProjectHandler))) handlerTypes.Add(t);
				if(t.IsSubclassOf(typeof(EcmProcessorBase))) processorTypes.Add(t);
			}
			myHandlerTypes = handlerTypes.ToArray();
			myProcessorTypes = processorTypes.ToArray();
		}

// コンストラクタ

		public Setting(){
			myStartTime = DateTime.Now;
		}

// 静的プロパティ

		[XmlIgnore]
		public static Assembly Assembly{
			get{return myAssembly;}
		}
		[XmlIgnore]
		public static Type[] HandlerTypes{
			get{return myHandlerTypes;}
		}
		[XmlIgnore]
		public static Type[] ProcessorTypes{
			get{return myProcessorTypes;}
		}


// 編集不能プロパティ

		[XmlIgnore]
		public DateTime StartTime{
			get{return myStartTime;}
		}

		// このプロジェクトのシステムファイル群が格納されるベースディレクトリを取得します。
		[XmlIgnore]
		public DirectoryInfo BaseDir{
			get{return myBaseFile.Directory;}
		}

		// このプロジェクトの作成日 (システムファイル群が格納されるベースディレクトリの作成日) を取得します。
		[XmlIgnore]
		public DateTime CreatedTime{
			get{
				if(this.BaseDir == null || !this.BaseDir.Exists) return DateTime.MinValue;
				return this.BaseDir.LastWriteTime;
			}
		}


		// この設定 XML ファイル自身を取得します。
		[XmlIgnore]
		public FileInfo BaseFile{
			get{return myBaseFile;}
			set{myBaseFile = value;}
		}

		// Id を設定・取得します。
		[XmlIgnore]
		public string Id{
			get{return myId;}
			set{myId = value;}
		}

		// ドキュメントディレクトリを取得します。
		[XmlIgnore]
		public DirectoryInfo DocumentFullPath{
			get{
				if(DocumentDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, DocumentDir));
			}
		}

		// テンプレートディレクトリを取得します。
		[XmlIgnore]
		public DirectoryInfo TemplateFullPath{
			get{
				if(TemplateDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, TemplateDir));
			}
		}

		// プレビュー画像ディレクトリを取得します。
		[XmlIgnore]
		public DirectoryInfo ImageFullPath{
			get{
				if(ImageDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, ImageDir));
			}
		}

		// スキーマディレクトリを取得します。
		[XmlIgnore]
		public DirectoryInfo SchemaDirInfo{
			get{
				if(SchemaDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, SchemaDir));
			}
		}

		// 外部データディレクトリを取得します。
		[XmlIgnore]
		public DirectoryInfo ExtDataDirInfo{
			get{
				if(SchemaDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, ExtDataDir));
			}
		}

		// CSV ファイルのパスを取得します。
		[XmlIgnore]
		public string CsvFullPath{
			get{
				if(CsvPath == null) return null;
				return Path.Combine(RootDir, CsvPath);
			}
		}


		[XmlIgnore]
		public Encoding HtmlEncodingObj{
			get{return Util.GetEncoding(HtmlEncoding);}
		}

		[XmlIgnore]
		public Encoding CsvEncodingObj{
			get{return Util.GetEncoding(CsvEncoding);}
		}

		[XmlIgnore]
		public string[] HiddenColumnList{
			get{
				if(myHiddenColumnList != null) return myHiddenColumnList;
				if(string.IsNullOrEmpty(myHiddenColumn)){
					myHiddenColumnList = new string[0];
				} else {
					string[] hiddenStrs = myHiddenColumn.Split(',');
					myHiddenColumnList = new string[hiddenStrs.Length];
					for(int i=0; i < hiddenStrs.Length; i++){
						myHiddenColumnList[i] = hiddenStrs[i].Trim();
					}
				}
				return myHiddenColumnList;
			}
		}


// 編集可能プロパティ

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.General)]
		[EccmDescription("プロジェクト名", "プロジェクトの名称を指定します。")]
		public string ProjectName{
			get{ return myProjectName; }
			set{ myProjectName = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("データ基準ディレクトリ", "データの基準となるディレクトリを指定します。DocumentDirなどの設定は、このディレクトリからの相対パスとして解釈されます。")]
		public string RootDir{
			get{ return myRootDir; }
			set{ myRootDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("CSVパス", "データファイルの場所を指定します。RootDir からの相対パス指定として解釈されます。")]
		public string CsvPath{
			get{ return myCsvPath; }
			set{ myCsvPath = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("ドキュメントディレクトリ", "HTML のデータが置かれるディレクトリを指定します。RootDir からの相対パス指定として解釈されます。")]
		public string DocumentDir{
			get{ return myDocumentDir; }
			set{ myDocumentDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("テンプレートディレクトリ","テンプレートのデータが置かれるディレクトリを指定します。RootDir からの相対パス指定として解釈されます。")]
		public string TemplateDir{
			get{ return myTemplateDir; }
			set{ myTemplateDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("スキーマディレクトリ", "XML Validation で使用するスキーマが置かれるディレクトリを指定します。RootDir からの相対パス指定として解釈されます。")]
		public string SchemaDir{
			get{ return mySchemaDir; }
			set{ mySchemaDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("外部データディレクトリ", "プラグイン等から使用する外部データが置かれるディレクトリを指定します。RootDir からの相対パス指定として解釈されます。")]
		public string ExtDataDir{ get; set; }

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("プレビュー画像ディレクトリ", "プレビュー画像を配置するディレクトリを指定します。RootDir からの相対パス指定として解釈されます。")]
		public string ImageDir{
			get{ return myImageDir; }
			set{ myImageDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Link)]
		[EccmDescription("CSVリンクパス", "CSVファイルを開くための URL を指定します。file: か http: で始まる絶対 URI を指定する必要があります。CSV が置かれているディレクトリが開くようにするのがオススメです。")]
		public string CsvLinkPath{
			get{ return myCsvLinkPath; }
			set{ myCsvLinkPath = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Link)]
		[EccmDescription("プレビュールートURL", "プレビュー環境のドキュメントルートを指す URL を指定します。絶対 URI を指定する必要があります。この URL はプレビュー用のリンクに使用されます。例 : http://mde7:8000/")]
		public string PreviewRootUrl{
			get{ return myPreviewRootUrl; }
			set{ myPreviewRootUrl = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("テンプレート拡張子", "標準のテンプレートファイルの拡張子を指定します。")]
		public string TemplateExt{
			get{ return myTemplateExt; }
			set{ myTemplateExt = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("テンプレート最大数", "一つのドキュメントで使用できるテンプレートの数の上限を指定します。パース中に処理したテンプレート数がこの数を超えると、無限ループと判断してパース処理を打ち切ります。")]
		public int DepthMax{
			get{ return myDepthMax; }
			set{ myDepthMax = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("アンカーテンプレート", "GetAnchor() 擬似メソッドで使用するアンカーの雛形を指定します。「{0}」と「{1}」がそれぞれコンテンツのパスおよびタイトルとなります。")]
		public string AnchorTemplate{
			get{ return myAnchorTemplate; }
			set{ myAnchorTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("自分リンクテンプレート", "GetAnchor() 擬似メソッドで使用する、自分リンク用の雛形を指定します。「{0}」がコンテンツのタイトルとなります。")]
		public string StayTemplate{
			get{ return myStayTemplate; }
			set{ myStayTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("絶対URLドメイン", "GetAbsUrlAnchor() 擬似メソッドで使用する、絶対URL生成のためのドメイン名を指定します。スキームを除いて指定する必要があります。")]
		public string AbsUrlDomain{
			get; set;
		}

		[EccmEditable("テンプレート内のECMコメントを削除する")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("ECMコメントの削除", "テンプレート内のECMコメントを削除するかどうかを指定します。true にすると削除します。")]
		public bool TemplateCommentDelete{
			get{ return myTemplateCommentDelete; }
			set{ myTemplateCommentDelete = value; }
		}

		[EccmEditable("パスを自動生成する")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("パス自動生成", "パス指定が無いとき、ID からファイル名を自動生成するかどうか指定します。true にすると /[ID].html をファイル名として処理します。")]
		public bool AutoPathGenerate{
			get{ return myAutoPathGenerate; }
			set{ myAutoPathGenerate = value; }
		}

		[EccmEditable("テンプレートの正規表現置換を有効にする")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("テンプレートの正規表現置換", "テンプレートファイル中の正規表現による「穴あきテンプレート」のサポートを有効にするかどうか指定します。true にすると有効になりますが、処理速度が低下するかもしれません。")]
		public bool EnableRegexTemplate{
			get{ return myEnableRegexTemplate; }
			set{ myEnableRegexTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("CSV文字符号化方式", "CSVファイルの文字符号化方式を指定します。Microsoft Excel 2003 が保存する CSV の場合は Shift_JIS となります。XMLスプレッドシートを利用する場合、この設定は無視されます。")]
		public string CsvEncoding{
			get{ return myCsvEncoding; }
			set{ myCsvEncoding = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("HTML文字符号化方式", "HTMLファイルの文字符号化方式を指定します。テンプレートファイル等もここで指定した文字符号化方式に合わせる必要があります。")]
		public string HtmlEncoding{
			get{ return myHtmlEncoding; }
			set{ myHtmlEncoding = value; }
		}

		[EccmEditable("meta charsetからの文字符号化方式判別を有効にする")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("meta charsetからの文字符号化方式判別", "Publish前のHTMLファイルのmeta charsetを読んで文字符号化方式を自動判別します。trueにすると有効になります。")]
		public bool HtmlEncodingDetectFromMetaCharset{
			get{ return myHtmlEncodingDetectFromMetaCharset; }
			set{ myHtmlEncodingDetectFromMetaCharset = value; }
		}
		
		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("デフォルトグローバルテンプレート", "デフォルトのグローバルテンプレートを指定することができます。個別に template の値が指定されていた場合はそちらが優先されます。")]
		public string DefaultGrobalTemplate{
			get{ return myDefaultGrobalTemplate; }
			set{ myDefaultGrobalTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("リンクの末尾から消す文字列", "Uri() メソッドなどで Uri が生成されたとき、末尾に指定文字列があれば削除します。「index.html」を指定すると、/ で終わるリンクが生成されたりします。")]
		public string IndexLinkSuffix{
			get{ return myIndexLinkSuffix; }
			set{ myIndexLinkSuffix = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("プラグインの言語", "プラグインの言語を None(なし) / CSharp(C#) / JScript (JScript.NET) から選択します。")]
		public PluginKind PluginKind{
			get{ return myPluginKind; }
			set{ myPluginKind = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("デフォルトソート列", "デフォルトでソートに使用する列を指定します。")]
		public string DefaultSortColumn{
			get{ return myDefaultSortColumn; }
			set{ myDefaultSortColumn = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("色分けルール", "表の色分けに使用する列名と正規表現を指定します。() で括られた部分が異なる場合に異なるグループとみなします。たとえば、title=(.{3}) と指定すると title の頭の3文字を比較して色分けします。列名を省略すると ID を比較します。")]
		public string ColorSeparateRule{
			get{ return myColorSeparateRule; }
			set{ myColorSeparateRule = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("色付けルール1", "表の色分けに使用する列名と正規表現を指定し、該当する行の色を変えます。たとえば、status=(完了|確認中) などと指定すると、該当する行にclass=\"pattern1\"がつきます。")]
		public string ColorRule1{ get; set;}


		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("色付けルール2", "表の色分けに使用する列名と正規表現を指定し、該当する行の色を変えます。たとえば、status=(完了|確認中) などと指定すると、該当する行にclass=\"pattern2\"がつきます。")]
		public string ColorRule2{ get; set;}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("色付けルール3", "表の色分けに使用する列名と正規表現を指定し、該当する行の色を変えます。たとえば、status=(完了|確認中) などと指定すると、該当する行にclass=\"pattern3\"がつきます。")]
		public string ColorRule3{ get; set;}


		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("非表示列", "非表示にしたい列名をカンマで区切って記述します。")]
		public string HiddenColumn{
			get{ return myHiddenColumn; }
			set{ myHiddenColumn = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("Parse許可条件", "Parseを許可する条件を指定します。無指定の場合は全て許可されます。設定例 : phase=1")]
		public string ParsePermissonRule{
			get{ return myParsePermissonRule; }
			set{ myParsePermissonRule = value; }
		}


		[EccmFieldGenre(EccmFieldGenreType.Misc)]
		[EccmDescription("シェルコマンド", "(この機能は未実装です。)「コマンド実行」で実行するコマンドの初期値を設定します。例 : svn up \\projects\\ECCM\\eccm_contents\\RMTL\\html")]
		public string SvnUpdateCommand{
			get{ return mySvnUpdateCommand; }
			set{ mySvnUpdateCommand = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Misc)]
		[EccmDescription("ECCMハンドラメニュー", "上部のメニューに表示する EccmHandler の型名をカンマ区切りで列挙して指定します。")]
		public string Handler{
			get{ return myHandler; }
			set{ myHandler = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Misc)]
		[EccmDescription("デフォルトスキーマ名", "XML Validation で使用するデフォルトのスキーマを指定します。schema列で値が指定されている場合、個々の指定の方が優先されます。")]
		public string DefaultSchemaName{
			get{ return myDefaultSchemaName; }
			set{ myDefaultSchemaName = value; }
		}


// 静的メソッド

		// Xml ファイルから Setting をロードします。
		public static Setting GetSetting(string targetFile){
			return GetSetting(new FileInfo(targetFile));
		}

		// Xml ファイルから Setting をロードします。
		public static Setting GetSetting(FileInfo targetFile){

			XmlSerializer xs = new XmlSerializer(typeof(Setting));
			Setting result;

			using(FileStream fs = targetFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				result = xs.Deserialize(fs) as Setting;
				fs.Close();
			}

			if(result == null) return null;
			result.Id = Path.GetFileNameWithoutExtension(targetFile.Name);
			result.BaseFile = targetFile;

			return result;
		}




// privateメソッド
		private string LoadData(string nodeName){
			XmlNodeList xnl = myXml.DocumentElement.GetElementsByTagName(nodeName);
			if(xnl.Count == 0) return null;
			return xnl[0].InnerText;
		}

		private string LoadDir(string nodeName){
			string s = LoadData(nodeName);
			if(String.IsNullOrEmpty(s)) return null;
			s = s.Replace('/', '\\');
			if(!s.EndsWith("\\")) s += '\\';
			return s;
		}

		private bool? LoadBoolData(string nodeName){
			string s = LoadData(nodeName);
			if(String.IsNullOrEmpty(s)) return null;
			if(s.Equals("true", StringComparison.CurrentCultureIgnoreCase)) return true;
			if(s.Equals("false", StringComparison.CurrentCultureIgnoreCase)) return false;
			return null;
		}

		private int? LoadIntData(string nodeName){
			string s = LoadData(nodeName);
			if(s == null) return null;
			int result = 0;
			try{
				result = Convert.ToInt32(s);
			} catch(FormatException){
				return null;
			}
			return result;
		}

	}



// カスタム属性


	// Setting のフィールドにこの属性を指定すると、編集画面で編集可能となります。
	// 引数は設定画面の description です。
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EccmEditableAttribute : Attribute{

		private string myLabel;

		public EccmEditableAttribute(string label){
			myLabel = label;	
		}
		public EccmEditableAttribute(){}

		// ラベルを取得します
		// bool値のチェックボックスのラベルとして使用されます。
		public string Label{
			get{return myLabel;}
		}

	}


	// Setting のフィールドのジャンルを指定する属性です。
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EccmFieldGenreAttribute : Attribute{

		private EccmFieldGenreType myGenre;
		public EccmFieldGenreAttribute(EccmFieldGenreType genre){
			myGenre = genre;	
		}

		public EccmFieldGenreType Genre{
			get{return myGenre;}
		}

	}

	public enum EccmFieldGenreType{
		General,
		Directory,
		Link,
		Parser,
		View,
		Misc
	}


	// Setting のフィールドにこの属性を指定すると、編集画面で編集可能となります。
	// 引数は設定画面の description です。
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EccmDescriptionAttribute : Attribute{

		private string myDescription;
		private string myName;

		public EccmDescriptionAttribute(string name, string desc){
			myName = name;	
			myDescription = desc;	
		}

		public string Name{
			get{return myName;}
		}

		public string Description{
			get{return myDescription;}
		}

		public override string ToString(){
			return myDescription;
		}
	}


}




