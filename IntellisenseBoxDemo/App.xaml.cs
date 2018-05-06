using SandBeige.IntellisenseBoxDemo.ViewModel;

using System.Windows;

namespace SandBeige.IntellisenseBoxDemo {
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			this.MainWindow = new MainWindow {
				DataContext = new MainWindowViewModel()
			};
			this.MainWindow.ShowDialog();
		}
	}
}
