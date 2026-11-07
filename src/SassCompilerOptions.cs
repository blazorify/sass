using System;
using System.Collections.Generic;

namespace Blazorify.Sass {
	public class SassCompilerOptions {
		/// <summary>
		/// Additional directories to resolve @import and @use from.
		/// </summary>
		public List<String> IncludePaths { get; set; } = [];

		/// <summary>
		/// Variables exposed to stylesheets as the "blazorify" module: <c>@use "blazorify" as v;</c> then <c>v.$name</c>.
		/// Values are written as raw Sass, so a quoted string needs its quotes, e.g. <c>"\"Inter\""</c>.
		/// </summary>
		public Dictionary<String, String> Variables { get; set; } = [];

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
