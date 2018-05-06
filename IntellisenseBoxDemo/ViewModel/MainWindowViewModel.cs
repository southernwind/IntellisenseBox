using SandBeige.IntellisenseBox;

using System.Collections.ObjectModel;
using System.Linq;

namespace SandBeige.IntellisenseBoxDemo.ViewModel {
	class MainWindowViewModel {
		public MainWindowViewModel() {
			this.Source = new ObservableCollection<ICandidate>(new[]
			{
				"aaa",
				"bbb",
				"ccc",
				"active",
				"apple",
				"acid",
				"access",
				"across"
			}.Select(x => new Candidate(x)));

			this.Source2 = new ObservableCollection<ICandidate>();
		}

		public ObservableCollection<ICandidate> Source {
			get;
		}

		public ObservableCollection<ICandidate> Source2 {
			get;
		}

		private string _currentText;

		public string CurrentText {
			get { return this._currentText; }
			set {
				if (this._currentText == value) {
					return;
				}

				this._currentText = value;
				this.Source2.Clear();
				foreach (var w in this.Source.Where(x => !x.Word.ToLower().Contains(this.CurrentText.ToLower()))) {
					this.Source2.Add(w);
				}
			}
		}
	}
}
