using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace we_helper {
	internal static class Utilities {
		public static int IndexOf<T>(this IEnumerable<T> list, T find, IEqualityComparer<T> comparer = null) {
			var findIsNull = find == null;
			return IndexOf(list, item => {
				if (item == null && findIsNull) return true;
				else if (comparer != null)
					return comparer.Equals(item, find);
				else if (findIsNull) return item.Equals(find);
				else return find.Equals(item);
			});
		}

		public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate) {
			var n = -1;
			foreach (var item in list) {
				n++;
				if (predicate(item))
					return n;
			}
			return -1;
		}

		public static async Task<string> ReadAllTextAsync(string file) {
			using (var filestream = System.IO.File.OpenText(file))
				return await filestream.ReadToEndAsync();
		}
		public static async Task WriteAllTextAsync(string file, string content, Encoding encoding = null) {
			using (var stream = new System.IO.StreamWriter(file, false, encoding ?? System.Text.Encoding.UTF8)) {
				await stream.WriteAsync(content);
			}
		}

		public static void SafeAdd(this ICollection<string> col, string input) {
			if (col.Contains(input)) return;
			lock (col)
				if (!col.Contains(input))
					col.Add(input);
		}
		public static void SafeRemove(this ICollection<string> col, string input) {
			if (!col.Contains(input)) return;
			lock (col)
				if (col.Contains(input))
					col.Remove(input);
		}
		public static string Join(this IEnumerable<string> list, string sep) {
			return string.Join(sep, list);
		}
		public static string NotEmpty(this string input, params string[] others) {
			if (!string.IsNullOrEmpty(input)) return input;
			return others.FirstOrDefault(x => !string.IsNullOrEmpty(x));
		}
		public static bool TryRemove<K, V>(this ConcurrentDictionary<K, V> dic, K key) {
			V value;
			return dic.TryRemove(key, out value);
		}

		public static void Add(this ConcurrentDictionary<string, HashSet<string>> collection, string key, string value) {
			collection.AddOrUpdate(key,
				_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { value },
				(_, hashset) => {
					if (!hashset.Contains(value)) hashset.Add(value);
					return hashset;
				});
		}

		public static Uri ToUri(this string input, Uri baseUri = null) {
			if (string.IsNullOrEmpty(input)) return null;

			Uri result;
			if (baseUri != null) {
				if (baseUri.IsFile) {
					input = input.Replace('/', '\\').TrimStart('\\');
				}
				if (Uri.TryCreate(baseUri, input, out result))
					return result;
			} else {
				if (System.IO.Directory.Exists(input) && !input.EndsWith("\\")) input += '\\';
				if (Uri.TryCreate(input, UriKind.Absolute, out result))
					return result;
			}
			return null;
		}

		public static async Task<IEnumerable<Tuple<string, string>>> TransformAsync(this IFileHelper helper, string file) {
			var content = await ReadAllTextAsync(file);
			return await helper.TransformAsync(file, content);
		}

		public static Task Async(Action action) {
			var task = new System.Threading.Tasks.Task(action);
			task.Start();
			return task;
		}

		public static string[] GetFiles(string directory, Regex rxFiles) {
			return GetFiles(directory, x => rxFiles.IsMatch(x));
		}

		public static string[] GetFiles(string directory, Func<string, bool> predicate) {
			return System.IO.Directory.GetDirectories(directory)
				.SelectMany(dir => GetFiles(dir, predicate))
				.Union(
					System.IO.Directory.GetFiles(directory)
						.Where(predicate)
				)
				.ToArray();
		}
	}

}
