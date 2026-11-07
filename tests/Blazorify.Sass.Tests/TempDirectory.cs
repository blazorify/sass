using System;
using System.IO;

namespace Blazorify.Sass.Tests {
	internal sealed class TempDirectory : IDisposable {
		public String Path { get; }

		public TempDirectory() {
			this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "blazorify sass " + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(this.Path);
		}

		public String Write(String relativePath, String content) {
			var fullPath = System.IO.Path.Combine(this.Path, relativePath);
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
			File.WriteAllText(fullPath, content);
			return fullPath;
		}

		public String Combine(String relativePath) {
			return System.IO.Path.Combine(this.Path, relativePath);
		}

		public void Dispose() {
			Directory.Delete(this.Path, true);
		}
	}
}
