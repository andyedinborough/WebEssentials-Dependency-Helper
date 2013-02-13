using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace we_helper {
	class Helper {
		public string Directory { get; private set; }
		public Uri BaseUri { get; private set; }
		private IFileHelper[] _Helpers;
		public Action<string> OnChanged { get; set; }
		public IEnumerable<FileDetail> Files {
			get {
				return _Files.Values.AsEnumerable();
			}
		}

		private Regex _rxFiles = new Regex(@"(\.min)?\.(css|js|less|ts)(\.bundle)?$", RegexOptions.Compiled);
		public Helper(string directory) {
			Directory = directory;
			BaseUri = directory.ToUri();

			var ihelperType = typeof(IFileHelper);
			_Helpers = ihelperType.Assembly.GetTypes()
				.Where(x => ihelperType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
				.Select(x => Activator.CreateInstance(x))
				.Cast<IFileHelper>()
				.ToArray();
			foreach (var fileHelper in _Helpers)
				fileHelper.Helper = this;
		}

		private ConcurrentDictionary<string, FileDetail> _Files = new ConcurrentDictionary<string, FileDetail>(StringComparer.OrdinalIgnoreCase);
		public void IndexFiles() {
			_Files.Clear();
			var files = Utilities.GetFiles(Directory, _rxFiles);
			foreach (var file in files) {
				IndexFile(file, FindHelper(file));
			}
		}

		private IFileHelper FindHelper(string file) {
			return _Helpers.FirstOrDefault(h => h.IsHelperFor(file));
		}

		public FileDetail IndexFile(string file, IFileHelper helper) {
			var content = System.IO.File.ReadAllText(file);
			var baseUri = System.IO.Path.GetDirectoryName(file).ToUri();
			var dependencies = helper == null
				? new Uri[0]
				: helper.FindDepenencies(content)
				.Select(x => x.ToUri(x.StartsWith("/") ? BaseUri : baseUri))
					.Where(x => x != null && x.IsFile)
					.ToArray();

			var detail = _Files.GetOrAdd(file, _ => new FileDetail(_, helper));
			detail.Dependencies.Clear();

			foreach (var dep in dependencies) {
				var sdep = dep.LocalPath;
				var hdep = FindHelper(sdep);
				var depDetail = _Files.GetOrAdd(sdep, _ => new FileDetail(_, hdep));
				detail.Dependencies.SafeAdd(sdep);
			}

			return detail;
		}

		public void FileChanged(string file) {
			if (!System.IO.File.Exists(file)) return;
			if (!_rxFiles.IsMatch(file)) return;

			var detail = IndexFile(file, FindHelper(file));
			if (detail == null) {
				_Files.TryRemove(file);
				return;
			}

			var last = System.IO.File.GetLastWriteTimeUtc(file);
			foreach (var dep in detail.Dependencies.ToArray()) {
				var deplast = System.IO.File.GetLastWriteTimeUtc(dep);
				if (deplast > last) last = deplast;
			}

			if (last == detail.Last) return;
			lock (detail) {
				if (last == detail.Last) return;
				detail.Last = last;
			}

			if (OnChanged != null)
				OnChanged(file);

			if (detail.FileHelper != null) {
				var results = detail.FileHelper.Transform(file);
				foreach (var result in results) {
					WriteIfDifferent(result.Item1, result.Item2);
					FileChangedAsync(result.Item1);
				}
			}

			var parents = FindFilesThatRequire(file);
			foreach (var parent in parents)
				FileChangedAsync(parent);
		}

		private string[] FindFilesThatRequire(string file) {
			return _Files.Where(x => x.Value.Dependencies.Contains(file)).Select(x => x.Key).ToArray();
		}

		private void FileChangedAsync(string file) {
			Utilities.Async(() => FileChanged(file));
		}

		public void FileDeleted(string file) {
			if (!_rxFiles.IsMatch(file)) return;
			FileDetail detail;
			if (!_Files.TryGetValue(file, out detail))
				return;

			_Files.TryRemove(file);
			var parents = FindFilesThatRequire(file);
			foreach (var parent in parents) {
				FileDetail parentDetail;
				if (!_Files.TryGetValue(parent, out parentDetail))
					continue;
				parentDetail.Dependencies.SafeRemove(file);
			}
		}

		public void FileRenamed(string old_file, string new_file) {
			FileDeleted(old_file);

			FileChanged(new_file);
		}

		private void WriteIfDifferent(string file, string content) {
			if (!System.IO.File.Exists(file)) {
				System.IO.File.WriteAllText(file, content);
				return;
			}
			var content0 = System.IO.File.ReadAllText(file);
			if (content == content0)
				return;
			System.IO.File.WriteAllText(file, content);
		}
	}

}
