using System;
using System.Collections.Generic;

namespace we_helper {
	class FileDetail {
		public FileDetail(string file, IFileHelper fileHelper) {
			File = file;
			FileHelper = fileHelper;
			Dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		public string File { get; set; }
		public IFileHelper FileHelper { get; set; }
		public HashSet<string> Dependencies { get; set; }
		public DateTime Last { get; set; }
	}

}
