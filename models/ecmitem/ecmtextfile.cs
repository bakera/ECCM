using System;
using System.IO;
using System.Text;


namespace Bakera.Eccm{
	public class EcmTextFile : EcmFileBase{
// コンストラクタ
		// フルパスを指定して EcmFile を作成します。
		public EcmTextFile(string path, EcmProject project) : base(path, project){}
	}
}

