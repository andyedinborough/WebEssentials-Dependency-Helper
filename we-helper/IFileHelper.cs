using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace we_helper {
	interface IFileHelper {
		Helper Helper { get; set; }
		bool IsHelperFor(string file);
		string[] FindDepenencies(string content);
		Task<IEnumerable<Tuple<string, string>>> TransformAsync(string file, string content);
	}
}
