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
		private EcmItem myCurrentItem = null; // ���݃p�[�X���̃I�u�W�F�N�g
		private EcmLog myLog = new EcmLog();
		private EcmProjectManager myManager;
		private List<string> myMiscErrors = new List<string>();
		private const string CsvCacheExt = ".dat";
		private const string XmlExt = ".xml";
		private const string MsXmlSheetNameSpace = "urn:schemas-microsoft-com:office:spreadsheet";


// �R���X�g���N�^
		public EcmProject(EcmProjectManager manager, Setting s){
			myManager = manager;
			mySetting = s;
			if(File.Exists(s.CsvFullPath)) LoadData();
		}

// �C���f�N�T
		// �񖼂���f�[�^���擾���܂��B
		public EcmItem this[string id]{
			get{
				DataRow r = GetRowById(id);
				if(r == null) return null;
				return new EcmItem(id, r);
			}
		}


// �v���p�e�B
		/// <summary>
		/// �f�[�^�̌�����Ԃ��܂��B
		/// </summary>
		public int DataCount{
			get{return this.Rows.Count;}
		}

		/// <summary>
		/// �f�[�^�����[�h/�X�V���ꂽ������Ԃ��܂��B
		/// ����� LoadCSV ���\�b�h���Ă΂ꂽ�Ƃ��̃t�@�C���̃^�C���X�^���v�ł��B
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
		/// ���ƂȂ�f�[�^�t�@�C����Ԃ��܂��B
		/// </summary>
		public FileInfo DataFile{
			get; private set;
		}

		/// <summary>
		/// �f�[�^���R�s�[�����e���|�����t�@�C����Ԃ��܂��B
		/// </summary>
		public FileInfo TempFile{
			get; private set;
		}

		/// <summary>
		/// �f�[�^�t�@�C���̃^�C���X�^���v��Ԃ��܂��B
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

		// ���� EcmProject �������� EcmProjectManager ���Q�Ƃ��܂��B
		public EcmProjectManager Manager{
			get{return myManager;}
		}


		// Setting ���擾���܂��B
		public Setting Setting{
			get{return mySetting;}
		}


		//
		public EcmItem CurrentItem{
			get{return myCurrentItem;}
			set{myCurrentItem = value;}
		}

		// �ʏ탍�O�p�� EcmLog ���擾���܂��B
		public EcmLog Log{
			get{return myLog;}
		}

		/// <summary>
		/// ID����擾���܂��B
		/// </summary>
		public DataColumn IdCol{
			get{
				return myIdCol;
			}
		}

		/// <summary>
		/// Path����擾���܂��B
		/// </summary>
		public DataColumn PathCol{
			get{
				return myPathCol;
			}
		}

		/// <summary>
		/// �v���W�F�N�g�����擾���܂��B
		/// </summary>
		public string ProjectName{
			get{
				return mySetting.ProjectName;
			}
		}

		/// <summary>
		/// �v���W�F�N�gID���擾���܂��B
		/// </summary>
		public string Id{
			get{
				return mySetting.Id;
			}
		}



// protect ���\�b�h

		protected override void OnRowChanged(DataRowChangeEventArgs e){
			base.OnRowChanged(e);
			this.ExtendedProperties["LoadTime"] = DateTime.Now;
		}
		protected override void OnRowDeleted(DataRowChangeEventArgs e){
			base.OnRowDeleted(e);
			this.ExtendedProperties["LoadTime"] = DateTime.Now;
		}



// public���\�b�h


		// �f�[�^�t�@�C�������[�h���܂��B
		public void LoadData(){
			DataFile = new FileInfo(mySetting.CsvFullPath);
			if(!DataFile.Exists) return;

			if(DataFile.Length > 50 * 1000 * 1000){
				myMiscErrors.Add(string.Format("�f�[�^�t�@�C���̃T�C�Y���� {0:n0} MB ����܂��B50MB�𒴂���t�@�C���͈����܂���B", DataFile.Length / (1000 * 1000)));
				return;
			}
			if(DataFile.Length > 10 * 1000 * 1000){
				myMiscErrors.Add(string.Format("�f�[�^�t�@�C���̃T�C�Y���� {0:n0} MB ����܂��B50MB�𒴂���ƈ����Ȃ��Ȃ�܂��B", DataFile.Length / (1000 * 1000)));
			}

			string tempFileName = DataFile.Name;
			TempFile = new FileInfo(mySetting.BaseDir.FullName + '\\' + tempFileName + CsvCacheExt);

			// �e���|�������ŐV�łȂ�������R�s�[���Ă���
			if(!TempFile.Exists || DataFile.LastWriteTime != TempFile.LastWriteTime) DataFileCopy(DataFile, TempFile);

			// �e���|�����t�@�C��������e��ǂ�
			if(DataFile.Extension.Equals(XmlExt, StringComparison.CurrentCultureIgnoreCase)){
				LoadXmlData(TempFile);
			} else {
				LoadCsvData(TempFile);
			}
		}

		// XML �t�@�C�������[�h���܂��B
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

				
				//�ŏ��̃V�[�g�����ǂ�
				XmlNodeList xmlSheets = xmlBook.GetElementsByTagName("Worksheet");
				if(xmlSheets.Count <= 0){
					myMiscErrors.Add("XML�̃��[�h�Ɏ��s���܂����BXML��Worksheet�v�f���܂܂�Ă��܂���B");
					return;
				}

				if(xmlSheets.Count > 1){
					Log.AddWarning("XML��Worksheet�v�f�������܂܂�Ă��܂��B�ŏ���Worksheet���g�p���܂��B" );
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
				myMiscErrors.Add("XML�̃��[�h�Ɏ��s���܂����B" + xmlExp.Message);
			}
		}

		// CSV �t�@�C�������[�h���܂��B
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

			// CSV�f�[�^�����[�h
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
				// �����ɂ���ď����Ȃ��Ƃ�������̂ň�x�����Ă݂�
				tempFile.Delete();
				dataFile.CopyTo(tempFile.FullName, true);
			}
		}


// �f�[�^�擾

		// ����s��ID���擾���܂��B
		public string GetIdByRow(DataRow row){
			return row[myIdCol] as string;
		}

		// ����s��Path���擾���܂��B
		public string GetPathByRow(DataRow row){
			if(myPathCol == null) return null;
			string result = row[myPathCol] as string;
			if(string.IsNullOrEmpty(result)) return null;

			// HTTP URI ���ǂ����`�F�b�N���A�h���C����������菜��
			Uri tryUri = null;
			bool uriCheckOk = Uri.TryCreate(result, UriKind.Absolute, out tryUri);
			if(uriCheckOk){
				result = tryUri.AbsolutePath;
			}

			return result;
		}


		// ����ID�̍s���擾���܂��B
		public DataRow GetRowById(string id){
			DataRow[] rows = GetDataByValue(myIdCol.ColumnName, id);
			if(rows.Length == 0) return null;
			return rows[0];
		}


		// ����ID�̂��閼�O�̃f�[�^���擾���܂��B
		public string GetDataById(string id, string colName){
			// �񂪖������ null ��Ԃ�
			if(this.Columns.IndexOf(colName) < 0) return null;
			DataRow r = GetRowById(id);
			if(r == null) return null;
			return GetRowById(id)[colName] as string;
		}

		// ��=�l �ł���悤�� DataRow�� Select ���܂��B
		public DataRow[] GetDataByValue(string colName, string dataValue){
			Log.AddInfo("DB����: ��={0}/�f�[�^��={1}", colName, dataValue);
			// �񂪖�����΋��Ԃ�
			if(this.Columns.IndexOf(colName) < 0) return new DataRow[0];
			colName = colName.Replace("[", "\\[").Replace("]", "\\]");
			dataValue = dataValue.Replace("'", "''");
			string selectStr = string.Format("[{0}]='{1}'", colName, dataValue);
			return this.Select(selectStr);
		}

		// ��=�l �ł���悤�� EcmItem �̔z����擾���܂��B
		public EcmItem[] GetItemsByValue(string colName, string dataValue){
			DataRow[] rows = GetDataByValue(colName, dataValue);
			EcmItem[] result = new EcmItem[rows.Length];
			for(int i = 0; i < rows.Length; i++){
				result[i] = new EcmItem(rows[i]);
			}
			return result;
		}

		// �n���ꂽ ID �ɑΉ����� EcmItem ���擾���܂��B���v���W�F�N�g�� ID �ɂ��Ή����܂��B
		// ID ����̏ꍇ�� currentItem ��Ԃ��܂��B
		public EcmItem GetItem(string id){
			if(string.IsNullOrEmpty(id)) return this.CurrentItem;

			string[] colonSeparated = id.Split(':');
			if(colonSeparated.Length > 1){
				string projectId = colonSeparated[0];
				string itemId = colonSeparated[1];
				// : �̉E�����󕶎���̏ꍇ�A���g��ID�Ɠ���EcmItem���Q�Ƃ���
				if(string.IsNullOrEmpty(itemId)) itemId = this.CurrentItem.Id;
				EcmProject targetProj = this.Manager.GetProject(projectId);
				if(targetProj != null){
					return targetProj.GetItem(itemId);
				}
			}

			return this[id];
		}


		// Table �ɑ����邷�ׂĂ� EcmItem ���擾���܂��B
		public EcmItem[] GetAllItems(){
			DataRowCollection rows = this.Rows;
			EcmItem[] result = new EcmItem[rows.Count];
			for(int i = 0; i < rows.Count; i++){
				result[i] = new EcmItem(rows[i]);
			}
			return result;
		}

		// �\�[�g���w�肵�āATable �ɑ����邷�ׂĂ� EcmItem ���擾���܂��B
		public EcmItem[] GetAllItems(string sortColName, bool reverse){
			return GetAllItems(null, sortColName, reverse);
		}

		// �i�荞�ݏ����ƃ\�[�g�������w�肵�āATable �ɑ����邷�ׂĂ� EcmItem ���擾���܂��B
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



		// ID �̏d�����`�F�b�N���܂��B
		// �d�����Ă���ID�̔z���Ԃ��܂��B
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


		// Path �̏d�����`�F�b�N���܂��B
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


		// ���̑��̃G���[�����o����Ă��Ȃ����`�F�b�N���܂��B
		public string[] GetMiscErrors(){
			return myMiscErrors.ToArray();
		}


		// �O���f�[�^�t�@�C�����擾���܂��B
		public FileInfo GetExtDataFile(string filename){
			FileInfo[] files = Setting.ExtDataDirInfo.GetFiles(filename);
			if(files.Length == 0) return null;
			return files[0];
		}


// private���\�b�h
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


		// XML ����s��ǂ�
		// ��̃Z���� Cell �v�f�����݂��Ȃ��̂Œ���
		private string[] GetDataFromXmlRow(XmlElement row){
			XmlNodeList cells = row.GetElementsByTagName("Cell", MsXmlSheetNameSpace);
			Dictionary<int, string> result = new Dictionary<int, string>();

			int index = 1; //�C���f�N�X�ԍ��B1����n�܂�
			foreach(XmlElement cell in cells){
				string cellResult = "";
				// �C���f�N�X�͒ʏ�C���N�������g����΂悢��
				// ss:Index�����Ŏw�肳��Ă��邱�Ƃ�����
				// �C���f�N�X�ԍ���1����n�܂�
				string indexAttr = cell.GetAttribute("Index", MsXmlSheetNameSpace);
				int indexAttrNumber = indexAttr.ToInt32();
				if(indexAttrNumber > 0){
					index = indexAttrNumber;
				}
				XmlNodeList dataElements = cell.GetElementsByTagName("Data", MsXmlSheetNameSpace);
				foreach(XmlElement dataElement in dataElements){
					// �R�����g�͖�������
					XmlElement p = dataElement.ParentNode as XmlElement;
					if(p.Name == "Comment") continue;
					cellResult += dataElement.InnerText;
				}
				result[index] = cellResult;
				index++;
			}
			// �ő�̃C���f�N�X�ԍ������߂�
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


		// DataColumn ���Z�b�g
		private void CreateDataCols(string[] datas){
			if(datas == null) return;
			
			DataColumn autoNumberingCol = new DataColumn("#", typeof(Int32));
			autoNumberingCol.AutoIncrement = true;
			autoNumberingCol.AutoIncrementSeed = 1;
			this.Columns.Add(autoNumberingCol);

			for(int i=0; i < datas.Length; i++){
				string s = datas[i];
				if(s == null){
					throw new Exception("�񖼂���ł��B");
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
			// ��̐������ǂ�
			// �f�[�^���������DBNull���i�[����
			// �ŏ��̗�̓i���o�����O�Ȃ̂ŁA�i�[�����C���f�N�X�̓f�[�^�C���f�N�X+1�ƂȂ�

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

			// �L���ȃf�[�^���Ȃ���΃X���[
			if(enableDataCounter <= 0) return;

			// Path ������ AutoPathGenerate ���L���̏ꍇ�APath ��ǉ�
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




