using System;
using System.Collections.Generic;

namespace we_helper {
	interface IFileHelper {
		Helper Helper { get; set; }
		bool IsHelperFor(string file);
		string[] FindDepenencies(string content);
		IEnumerable<Tuple<string, string>> Transform(string file, string content);
	}
}
