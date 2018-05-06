namespace SandBeige.IntellisenseBox {
	public class Candidate : ICandidate {
		public Candidate(string word) {
			this.Word = word;
		}

		public string Word { get; }
	}
}
