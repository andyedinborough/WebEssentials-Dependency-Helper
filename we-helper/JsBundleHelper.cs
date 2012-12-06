
namespace we_helper {
	//class JsBundleHelper : IFileHelper {
	//	public static Func<string, string> MinifyJs { get; set; }
	//	public static Func<string, string> SourceMapJs { get; set; }
	//	public Helper Helper { get; set; }

	//	public bool IsHelperFor(string file) {
	//		return file.EndsWith(".js.bundle", StringComparison.OrdinalIgnoreCase);
	//	}
	//	public string[] FindDepenencies(string content) {
	//		var xdoc = XDocument.Parse(content);
	//		return xdoc.Descendants("file").Select(x => x.Value.Trim()).ToArray();
	//	}
	//	public IEnumerable<Tuple<string, string>> Transform(string file, string content) {
	//		var js = FindDepenencies(content)
	//			.Select(x => "//#source " + x + Environment.NewLine + System.IO.File.ReadAllText(x.ToUri(Helper.BaseUri).LocalPath))
	//			.Join(Environment.NewLine);
	//		var baseFile = file.Substring(0, file.Length - ".js.bundle".Length);
	//		return new[]{
	//			Tuple.Create(baseFile + ".js", js),
	//			Tuple.Create(baseFile + ".min.js", MinifyJs(js)),
	//			Tuple.Create(baseFile + ".minjs.map", SourceMapJs(js)),
	//		};
	//	}
	//}
}
