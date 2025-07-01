using System.Collections.Generic;

namespace Blazorify.Sass {
	public class SassBuildManager {
		private readonly List<SassFileRegistration> files = [];

		public IReadOnlyList<SassFileRegistration> RegisteredFiles => this.files;

		public void Register(SassFileRegistration file) {
			this.files.Add(file);
		}
	}
}
