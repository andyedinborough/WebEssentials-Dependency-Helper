using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace we_helper {
	class LessHelper : IFileHelper {
		public static Func<string, string, string> CompileLess { get; set; }
		private Regex rxImports = new Regex(@"@import\s+(((?<quote>['""])(?<file>.+?)\<quote>)|(url\s*\((?<quote>['""])(?<file>.+?)\<quote>))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public Helper Helper { get; set; }

		public bool IsHelperFor(string file) {
			return file.EndsWith(".less", StringComparison.OrdinalIgnoreCase);
		}

		public string[] FindDepenencies(string content) {
			var exts = new HashSet<string> { "css", "less" };
			return rxImports.Matches(content)
				.Cast<Match>()
				.Select(x => x.Groups["file"].Value)
				.Select(x => exts.Contains(x.Split('.').Last().ToLower()) ? x : (x + ".less"))
				.ToArray();
		}

		public IEnumerable<Tuple<string, string>> Transform(string file, string content) {
			var css = CompileLess(file, content);
			var baseFile = file.Substring(0, file.Length - ".less".Length);
			return new[]{
				Tuple.Create(baseFile + ".css", css),
				Tuple.Create(baseFile + ".min.css", CssBundleHelper.MinifyCss(css) ),
			};
		}
	}

}
