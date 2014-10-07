using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Xml;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;



namespace Bakera.Eccm{
	public partial class EcmProject : DataTable{

		private Regex csvFragmentReg = new EcmRegex.CsvFragment();
		private DataColumn myIdCol = null;
		private DataColumn myPathCol = null;
		private Setting mySetting = null;
		private EcmItem myCurrentItem = null; // 現在パース中のオブジェクト
		private EcmLog myLog = new EcmLog();
		private EcmProjectManager myManager;
		private List<string> myMiscErrors = new List<string>();
		private const string CsvCacheExt = ".dat";
		private const string XmlExt = ".xml";
		private const string MsXmlSheetNameSpace = "urn:schemas-microsoft-com:office:spreadsheet";


// コンストラクタ
		public EcmProject(EcmProjectManager manager, Setting s){
			myManager = manager;
			mySetting = s;
			if(File.Exists(s.CsvFullPath)) LoadData();
		}

// インデクサ
		// 列名からデータを取得します。
		public EcmItem this[string id]{
			get{
				DataRow r = GetRowById(id);
				if(r == null) return null;
				return new EcmItem(id, r);
			}
		}


// プロパティ
		/// <summary>
		/// データの件数を返します。
		/// </summary>
		public int DataCount{
			get{return this.Rows.Count;}
		}

		/// <summary>
		/// データがロード/更新された時刻を返します。
		/// これは LoadCSV メソッドが呼ばれたときのファイルのタイムスタンプです。
		/// </summary>
		public DateTime DataTime{
			get{
				if(this.ExtendedProperties["LoadTime"] == null){
					return default(DateTime);
				}
				return (DateTime)this.ExtendedProperties["LoadTime"];
			}
		}

		/// <summary>
		/// 元となるデータファイルを返します。
		/// </summary>
		public FileInfo DataFile{
			get; private set;
		}

		/// <summary>
		/// データをコピーしたテンポラリファイルを返します。
		/// </summary>
		public FileInfo TempFile{
			get; private set;
		}

		/// <summary>
		/// データファイルのタイムスタンプを返します。
		/// </summary>
		public DateTime FileTime{
			get{
				if(DataFile.Exists) {
					DataFile.Refresh();
					return DataFile.LastWriteTime;
				}
				return default(DateTime);
			}
		}

		// この EcmProject が属する EcmProjectManager を参照します。
		public EcmProjectManager Manager{
			get{return myManager;}
		}


		// Setting を取得します。
		public Setting Setting{
			get{return mySetting;}
		}


		//
		public EcmItem CurrentItem{
			get{return myCurrentItem;}
			set{myCurrentItem = value;}
		}

		// 通常ログ用の EcmLog を取得します。
		public EcmLog Log{
			get{return myLog;}
		}

		/// <summary>
		/// ID列を取得します。
		/// </summary>
		public DataColumn IdCol{
			get{
				return myIdCol;
			}
		}

		/// <summary>
		/// Path列を取得します。
		/// </summary>
		public DataColumn PathCol{
			get{
				return myPathCol;
			}
		}

		/// <summary>
		/// プロジェクト名を取得します。
		/// </summary>
		public string ProjectName{
			get{
				return mySetting.ProjectName;
			}
		}

		/// <summary>
		/// プロジェクトIDを取得します。
		/// </summary>
		public string Id{
			get{
				return mySetting.Id;
			}
		}



// protect メソッド

		protected override void OnRowChanged(DataRowChangeEventArgs e){
			base.OnRowChanged(e);
			this.ExtendedProperties["LoadTime"] = DateTime.Now;
		}
		protected override void OnRowDeleted(DataRowChangeEventArgs e){
			base.OnRowDeleted(e);
			this.ExtendedProperties["LoadTime"] = DateTime.Now;
		}



// publicメソッド


		// データファイルをロードします。
		public void LoadData(){
			DataFile = new FileInfo(mySetting.CsvFullPath);
			if(!DataFile.Exists) return;

			if(DataFile.Length > 50 * 1000 * 1000){
				myMiscErrors.Add(string.Format("データファイルのサイズが約 {0:n0} MB あります。50MBを超えるファイルは扱えません。", DataFile.Length / (1000 * 1000)));
				return;
			}
			if(DataFile.Length > 10 * 1000 * 1000){
				myMiscErrors.Add(string.Format("データファイルのサイズが約 {0:n0} MB あります。50MBを超えると扱えなくなります。", DataFile.Length / (1000 * 1000)));
			}

			string tempFileName = DataFile.Name;
			TempFile = new FileInfo(mySetting.BaseDir.FullName + '\\' + tempFileName + CsvCacheExt);

			// テンポラリが最新でなかったらコピーしてくる
			if(!TempFile.Exists || DataFile.LastWriteTime != TempFile.LastWriteTime) DataFileCopy(DataFile, TempFile);

			// テンポラリファイルから内容を読む
			if(DataFile.Extension.Equals(XmlExt, StringComparison.CurrentCultureIgnoreCase)){
				LoadXmlData(TempFile);
			} else {
				LoadCsvData(TempFile);
			}
		}

		// XML ファイルをロードします。
		private void LoadXmlData(FileInfo xmlFile){
			try{
				this.TableName = xmlFile.FullName;
				this.ExtendedProperties["LoadTime"] = xmlFile.LastWriteTime;

				XmlDocument xmlBook = new XmlDocument();
				xmlBook.XmlResolver = null;

				using(FileStream fs = xmlFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
					xmlBook.Load(fs);
					fs.Close();
				}

				
				//最初のシートだけ読む
				XmlNodeList xmlSheets = xmlBook.GetElementsByTagName("Worksheet");
				if(xmlSheets.Count <= 0){
					myMiscErrors.Add("XMLのロードに失敗しました。XMLにWorksheet要素が含まれていません。");
					return;
				}

				if(xmlSheets.Count > 1){
					Log.AddWarning("XMLにWorksheet要素が複数含まれています。最初のWorksheetを使用します。" );
				}


				XmlElement xmlSheet = xmlSheets[0] as XmlElement;
				XmlNodeList xmlRows = xmlSheet.GetElementsByTagName("Row");
				bool firstLineFlag = true;
				for(int i=0; i < xmlRows.Count; i++){
					string[] datas = GetDataFromXmlRow(xmlRows[i] as XmlElement);
					if(firstLineFlag){
						CreateDataCols(datas);
						firstLineFlag = false;
					} else {
						AddDatas(datas);
					}
				}
			} catch(XmlException xmlExp){
				myMiscErrors.Add("XMLのロードに失敗しました。" + xmlExp.Message);
			}
		}

		// CSV ファイルをロードします。
		private void LoadCsvData(FileInfo csvFile){
			this.TableName = csvFile.FullName;
			this.ExtendedProperties["LoadTime"] = csvFile.LastWriteTime;

			string csvFileData = null;
			using(FileStream fs = csvFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, Setting.CsvEncodingObj)){
					csvFileData = sr.ReadToEnd();
					sr.Close();
				}
				fs.Close();
			}

			if(string.IsNullOrEmpty(csvFileData)) return;

			// CSVデータをロード
			string[] csvFileLines = csvFileData.Split(new string[]{"\x0d\x0a", "\x0a", "\x0d"}, StringSplitOptions.RemoveEmptyEntries);
			bool firstLineFlag = true;

			for(int i=0; i < csvFileLines.Length; i++){
				string line = csvFileLines[i];
				while (countChars(line, '"') % 2 > 0 && ++i < csvFileLines.Length) {
					line += "\n";
					line += csvFileLines[i];
				}
				line += ',';
				string[] datas = GetDataFromLine(line);

				if(firstLineFlag){
					CreateDataCols(datas);
					firstLineFlag = false;
				} else {
					AddDatas(datas);
				}
			}
		}

		private void DataFileCopy(FileInfo dataFile, FileInfo tempFile){
			if(!dataFile.Exists) return;
			if(tempFile.Exists && tempFile.IsReadOnly){
				tempFile.IsReadOnly = false;
			}
			try{
				dataFile.CopyTo(tempFile.FullName, true);
			} catch (IOException){
				// 権限によって書けないときがあるので一度消してみる
				tempFile.Delete();
				dataFile.CopyTo(tempFile.FullName, true);
			}
		}


// データ取得

		// ある行のIDを取得します。
		public string GetIdByRow(DataRow row){
			return row[myIdCol] as string;
		}

		// ある行のPathを取得します。
		public string GetPathByRow(DataRow row){
			if(myPathCol == null) return null;
			string result = row[myPathCol] as string;
			if(string.IsNullOrEmpty(result)) return null;

			// HTTP URI かどうかチェックし、ドメイン部分を取り除く
			Uri tryUri = null;
			bool uriCheckOk = Uri.TryCreate(result, UriKind.Absolute, out tryUri);
			if(uriCheckOk){
				result = tryUri.AbsolutePath;
			}

			return result;
		}


		// あるIDの行を取得します。
		public DataRow GetRowById(string id){
			DataRow[] rows = GetDataByValue(myIdCol.ColumnName, id);
			if(rows.Length == 0) return null;
			return rows[0];
		}


		// あるIDのある名前のデータを取得します。
		public string GetDataById(string id, string colName){
			// 列が無ければ null を返す
			if(this.Columns.IndexOf(colName) < 0) return null;
			DataRow r = GetRowById(id);
			if(r == null) return null;
			return GetRowById(id)[colName] as string;
		}

		// 列名=値 であるような DataRowを Select します。
		public DataRow[] GetDataByValue(string colName, string dataValue){
			Log.AddInfo("DB検索: 列名={0}/データ名={1}", colName, dataValue);
			// 列が無ければ空を返す
			if(this.Columns.IndexOf(colName) < 0) return new DataRow[0];
			colName = colName.Replace("[", "\\[").Replace("]", "\\]");
			dataValue = dataValue.Replace("'", "''");
			string selectStr = string.Format("[{0}]='{1}'", colName, dataValue);
			return this.Select(selectStr);
		}

		// 列名=値 であるような EcmItem の配列を取得します。
		public EcmItem[] GetItemsByValue(string colName, string dataValue){
			DataRow[] rows = GetDataByValue(colName, dataValue);
			EcmItem[] result = new EcmItem[rows.Length];
			for(int i = 0; i < rows.Length; i++){
				result[i] = new EcmItem(rows[i]);
			}
			return result;
		}

		// 渡された ID に対応する EcmItem を取得します。他プロジェクトの ID にも対応します。
		// ID が空の場合は currentItem を返します。
		public EcmItem GetItem(string id){
			if(string.IsNullOrEmpty(id)) return this.CurrentItem;

			string[] colonSeparated = id.Split(':');
			if(colonSeparated.Length > 1){
				string projectId = colonSeparated[0];
				string itemId = colonSeparated[1];
				// : の右側が空文字列の場合、自身のIDと同じEcmItemを参照する
				if(string.IsNullOrEmpty(itemId)) itemId = this.CurrentItem.Id;
				EcmProject targetProj = this.Manager.GetProject(projectId);
				if(targetProj != null){
					return targetProj.GetItem(itemId);
				}
			}

			return this[id];
		}


		// Table に属するすべての EcmItem を取得します。
		public EcmItem[] GetAllItems(){
			DataRowCollection rows = this.Rows;
			EcmItem[] result = new EcmItem[rows.Count];
			for(int i = 0; i < rows.Count; i++){
				result[i] = new EcmItem(rows[i]);
			}
			return result;
		}

		// ソートを指定して、Table に属するすべての EcmItem を取得します。
		public EcmItem[] GetAllItems(string sortColName, bool reverse){
			return GetAllItems(null, sortColName, reverse);
		}

		// 絞り込み条件とソート条件を指定して、Table に属するすべての EcmItem を取得します。
		public EcmItem[] GetAllItems(string filterExpression, string sortColName, bool reverse){
			if(this.Columns.Count == 0) return null;

			if(string.IsNullOrEmpty(sortColName) && string.IsNullOrEmpty(filterExpression)){
				return GetAllItems();
			}

			DataRow[] rows = null;
			if(string.IsNullOrEmpty(sortColName)){
				rows = this.Select(filterExpression);
			} else {
				if(reverse) sortColName += " DESC";
				rows = this.Select(filterExpression, sortColName);
			}

			EcmItem[] result = new EcmItem[rows.Length];
			for(int i = 0; i < rows.Length; i++){
				result[i] = new EcmItem(rows[i]);
			}
			return result;
		}



		// ID の重複をチェックします。
		// 重複しているIDの配列を返します。
		public string[] GetDuplicateId(){
			var result = new List<string>();
			foreach(DataRow r in this.Rows){
				string id = r[myIdCol] as string;
				if(string.IsNullOrEmpty(id)) continue;
				if(result.Contains(id)) continue;
				DataRow[] rows = GetDataByValue(myIdCol.ColumnName, id);
				if(rows.Length > 1) result.Add(id);
			}
			return result.ToArray();
		}


		// Path の重複をチェックします。
		public string[] GetDuplicatePath(){
			if(myPathCol == null) return new string[0];
			var result = new List<string>();
			foreach(DataRow r in this.Rows){
				string path = r[myPathCol] as string;
				if(string.IsNullOrEmpty(path)) continue;
				if(result.Contains(path)) continue;
				DataRow[] rows = GetDataByValue(myPathCol.ColumnName, path);
				if(rows.Length > 1) result.Add(path);
				
			}
			return result.ToArray();
		}


		// その他のエラーが検出されていないかチェックします。
		public string[] GetMiscErrors(){
			return myMiscErrors.ToArray();
		}


		// 外部データファイルを取得します。
		public FileInfo GetExtDataFile(string filename){
			FileInfo[] files = Setting.ExtDataDirInfo.GetFiles(filename);
			if(files.Length == 0) return null;
			return files[0];
		}


// privateメソッド
		private string[] GetDataFromLine(string line){
			MatchCollection matches = csvFragmentReg.Matches(line);
			string[] result = new string[matches.Count];

			for(int i=0; i<matches.Count; i++){
				string val = matches[i].Groups[1].Value;
				if(val.StartsWith("\"")) val = val.Remove(0,1);
				if(val.EndsWith("\"")) val = val.Remove(val.Length-1);
				val = val.Replace("\"\"", "\"");
				result[i] = val.Trim();
			}
			return result;
		}


		// XML から行を読む
		// 空のセルは Cell 要素が存在しないので注意
		private string[] GetDataFromXmlRow(XmlElement row){
			XmlNodeList cells = row.GetElementsByTagName("Cell", MsXmlSheetNameSpace);
			Dictionary<int, string> result = new Dictionary<int, string>();

			int index = 1; //インデクス番号。1から始まる
			foreach(XmlElement cell in cells){
				string cellResult = "";
				// インデクスは通常インクリメントすればよいが
				// ss:Index属性で指定されていることもある
				// インデクス番号は1から始まる
				string indexAttr = cell.GetAttribute("Index", MsXmlSheetNameSpace);
				int indexAttrNumber = indexAttr.ToInt32();
				if(indexAttrNumber > 0){
					index = indexAttrNumber;
				}
				XmlNodeList dataElements = cell.GetElementsByTagName("Data", MsXmlSheetNameSpace);
				foreach(XmlElement dataElement in dataElements){
					// コメントは無視する
					XmlElement p = dataElement.ParentNode as XmlElement;
					if(p.Name == "Comment") continue;
					cellResult += dataElement.InnerText;
				}
				result[index] = cellResult;
				index++;
			}
			// 最大のインデクス番号を求める
			int indexmax = 0;
			foreach(int key in result.Keys){
				if(indexmax < key) indexmax = key;
			}
			string[] resultString = new string[indexmax];
			foreach(int key in result.Keys){
				resultString[key-1] = result[key];
			}
			return resultString;
		}


		// DataColumn をセット
		private void CreateDataCols(string[] datas){
			if(datas == null) return;
			
			DataColumn autoNumberingCol = new DataColumn("#", typeof(Int32));
			autoNumberingCol.AutoIncrement = true;
			autoNumberingCol.AutoIncrementSeed = 1;
			this.Columns.Add(autoNumberingCol);

			for(int i=0; i < datas.Length; i++){
				string s = datas[i];
				if(s == null){
					throw new Exception("列名が空です。");
				}
				DataColumn col = new DataColumn(s, typeof(string));
				col.AllowDBNull = true;
				this.Columns.Add(col);
				if(ColNameMatch(s, EcmItem.ReservedIdName)){
					myIdCol = col;
				} else if(ColNameMatch(s, EcmItem.ReservedPathName)){
					myPathCol = col;
				}
			}

			if(myPathCol == null){
				if(mySetting.AutoPathGenerate == true){
					myPathCol = new DataColumn(EcmItem.ReservedMark + EcmItem.ReservedPathName, typeof(string));
					this.Columns.Add(myPathCol);
				}
			}
			this.PrimaryKey = new DataColumn[]{autoNumberingCol};
		}


		public static bool ColNameMatch(string s, string colname){
			return (s.Equals(colname, StringComparison.CurrentCultureIgnoreCase) || s.Equals(EcmItem.ReservedMark + colname, StringComparison.CurrentCultureIgnoreCase));
		}


		private void AddDatas(string[] datas){
			DataRow row = this.NewRow();
			// 列の数だけ読む
			// データが無ければDBNullを格納する
			// 最初の列はナンバリングなので、格納する列インデクスはデータインデクス+1となる

			int enableDataCounter = 0;

			for(int i = 1; i < this.Columns.Count; i++){
				if(i <= datas.Length){
					string data = datas[i-1];
					if(string.IsNullOrEmpty(data)){
						row[i] = DBNull.Value;
					} else {
						row[i] = datas[i-1];
						enableDataCounter++;
					}
				} else {
					row[i] = DBNull.Value;
				}
			}

			// 有効なデータがなければスルー
			if(enableDataCounter <= 0) return;

			// Path が無く AutoPathGenerate が有効の場合、Path を追加
			if(myPathCol != null && myIdCol != null && row[myPathCol] == DBNull.Value && mySetting.AutoPathGenerate == true){
				row[myPathCol] = "/" + row[myIdCol].ToString().ToLower() + ".html";
			}
			this.Rows.Add(row);
			return;
		}


		private int countChars(string str, char c){
			int result = 0;
			foreach(char tmp in str){
				if(tmp == c) result++;
			}
			return result;
		
		}


	}
}




