using System;
using System.IO;
using System.Threading;

namespace Blazorify.Sass.Tests {
	internal static class Eventually {
		public static String ReadWhen(String path, Func<String, Boolean> condition) {
			var deadline = DateTime.UtcNow.AddSeconds(15);
			var last = "";

			while (DateTime.UtcNow < deadline) {
				if (File.Exists(path)) {
					try {
						last = File.ReadAllText(path);
						if (condition(last)) {
							return last;
						}
					} catch (IOException) {
						// Sass may still be writing the file.
					}
				}

				Thread.Sleep(100);
			}

			return last;
		}
	}
}
