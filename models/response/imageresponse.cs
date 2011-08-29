using System;
using System.IO;
using System.Web;
using System.Web.Configuration;
using System.Drawing;

namespace Bakera.Eccm{

	public class ImageResponse : EcmResponse{

		private FileInfo myFile;


// コンストラクタ
		public ImageResponse(FileInfo file){
			myFile = file;
		}


// プロパティ

		public override string ContentType{
			get{
				switch(myFile.Extension.ToLower()){
				case ".png":
					return "image/png";
				case ".jpg":
				case ".jpeg":
					return "image/jpeg";
				case ".gif":
					return "image/gif";
				default:
					return "UNKNOWN:" + myFile.Extension;
				}
			}
		}

		public override void WriteResponse(HttpResponse response){
			response.ContentType = this.ContentType;
			if(myFile.Exists) response.WriteFile(myFile.FullName);
		}
	}

}

