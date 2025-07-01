using System;
using System.Collections.Generic;

namespace Blazorify.Sass {
	public class SassCompilerOptions {
		/// <summary>
		/// Additional directories to resolve @import and @use from.
		/// </summary>
		public List<String> IncludePaths { get; set; } = [];

		/// <summary>
		/// Output style. Options: "expanded", "compressed".
		/// </summary>
		public String Style { get; set; } = "expanded";

		/// <summary>
		/// Whether to generate source maps.
		/// </summary>
		public Boolean SourceMap { get; set; } = false;

		/// <summary>
		/// Whether to suppress warnings.
		/// </summary>
		public Boolean Quiet { get; set; } = true;
	}
}
