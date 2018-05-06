using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace SandBeige.IntellisenseBox {
	public class IntellisenseBox : TextBox {
		private const string PartPopup = "PART_Popup";
		private const string PartSuggest = "PART_Suggest";
		private const string PartTextBox = "PART_TextBox";

		private Popup _popup;
		private ListBox _suggest;
		private TextBox _textBox;

		/// <summary>
		/// サジェストのコレクションビュー参照
		/// </summary>
		private ICollectionView _collectionView;

		#region Separator 依存関係プロパティ

		public static readonly DependencyProperty SeparatorProperty =
			DependencyProperty.Register(
				nameof(Separator),
				typeof(string),
				typeof(IntellisenseBox),
				new PropertyMetadata(" .,\t\r\n"));

		/// <summary>
		/// サジェスト検索対象文字列を区切る文字をつないだ文字列
		/// </summary>
		public string Separator {
			get { return (string)this.GetValue(SeparatorProperty); }
			set { this.SetValue(SeparatorProperty, value); }
		}

		#endregion

		#region SuggestSource 依存関係プロパティ

		public static readonly DependencyProperty SuggestSourceProperty =
			DependencyProperty.Register(nameof(SuggestSource),
				typeof(IEnumerable<ICandidate>),
				typeof(IntellisenseBox),
				new UIPropertyMetadata(null, SetCollectionView));

		/// <summary>
		/// サジェスト候補
		/// </summary>
		public IEnumerable<ICandidate> SuggestSource {
			get { return (IEnumerable<ICandidate>)this.GetValue(SuggestSourceProperty); }
			set {
				this.SetValue(SuggestSourceProperty, value);
			}
		}

		#endregion

		#region CurrentText 依存関係プロパティ

		public static readonly DependencyProperty CurrentTextProperty =
			DependencyProperty.Register(
				nameof(CurrentText),
				typeof(string),
				typeof(IntellisenseBox),
				new PropertyMetadata(null));

		// public static readonly DependencyProperty CurrentTextProperty = CurrentTextPropertyKey.DependencyProperty;

		/// <summary>
		/// サジェスト候補検索用文字列
		/// </summary>
		public string CurrentText {
			get { return (string)this.GetValue(CurrentTextProperty); }
			set {
				this.SetValue(CurrentTextProperty, value);
			}
		}

		#endregion

		#region Mode 依存関係プロパティ

		public enum IntellisenseMode {
			Auto,
			Manual
		}

		public static readonly DependencyProperty ModeProperty =
			DependencyProperty.Register(nameof(Mode),
				typeof(IntellisenseMode),
				typeof(IntellisenseBox),
				new UIPropertyMetadata(IntellisenseMode.Auto, SetCollectionView));

		/// <summary>
		/// サジェスト選択モード
		/// <see cref="IntellisenseMode.Auto"/>の場合、サジェスト候補の中から、サジェスト候補検索用文字列を含むものをサジェストとして出す
		/// <see cref="IntellisenseMode.Manual"/> の場合、サジェスト候補をそのまま出す
		/// </summary>
		public IntellisenseMode Mode {
			get { return (IntellisenseMode)this.GetValue(ModeProperty); }
			set {
				this.SetValue(ModeProperty, value);
			}
		}

		#endregion

		#region IgnoreCase 依存関係プロパティ

		public static readonly DependencyProperty IgnoreCaseProperty =
			DependencyProperty.Register(
				nameof(IgnoreCase),
				typeof(bool),
				typeof(IntellisenseBox),
				new UIPropertyMetadata(true));

		/// <summary>
		/// <see cref="IntellisenseMode.Auto"/>の場合に大文字小文字の区別を無視するかどうか
		/// </summary>
		public bool IgnoreCase {
			get { return (bool)this.GetValue(IgnoreCaseProperty); }
			set {
				this.SetValue(IgnoreCaseProperty, value);
			}
		}

		#endregion

		static IntellisenseBox() {
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(IntellisenseBox),
				new FrameworkPropertyMetadata(typeof(IntellisenseBox)));
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			this._popup = (Popup)this.GetTemplateChild(PartPopup);
			this._suggest = (ListBox)this.GetTemplateChild(PartSuggest);
			this._textBox = (TextBox)this.GetTemplateChild(PartTextBox);

			this._suggest.PreviewKeyDown += this.IntellisenseBoxPreviewKeyDown;
			this._textBox.PreviewKeyDown += this.IntellisenseBoxPreviewKeyDown;
			this._suggest.MouseDoubleClick += (sender, e) => this.DecideWord();
		}

		/// <summary>
		/// コレクションビューの取得、フィルタの設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void SetCollectionView(object sender, DependencyPropertyChangedEventArgs e) {
			if (!(sender is IntellisenseBox ib)) {
				return;
			}

			if (ib.Mode != IntellisenseMode.Auto) {
				ib._collectionView = null;
				return;
			}
			ib._collectionView = CollectionViewSource.GetDefaultView(ib.SuggestSource);
			ib._collectionView.Filter = ib.Filter;
		}

		/// <summary>
		/// キー押下
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void IntellisenseBoxPreviewKeyDown(object sender, KeyEventArgs e) {
			if (ReferenceEquals(sender, this._suggest)) {
				// サジェスト上でのキー操作
				switch (e.Key) {
					case Key.Up:
					case Key.Down:
						return;
					case Key.Enter:
					case Key.Tab:
					case Key.Space:
						this.DecideWord();
						e.Handled = true;
						return;
				}
				this._textBox.Focus();
			}

			// 以下テキストボックスへフォーカスが移ったあとの動作

			if (this._popup.IsOpen) {
				switch (e.Key) {
					case Key.Escape:
						this.SuggestClose();
						return;
					case Key.Tab:
					case Key.Up:
					case Key.Down:
						this._suggest.Focus();
						return;
					case Key.Enter:
						if (this._suggest.SelectedIndex == -1) {
							this._suggest.SelectedIndex = 0;
						}
						this.DecideWord();
						e.Handled = true;
						return;
				}
			}

			var oldText = this._textBox.Text;
			this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
				new Action(() => {
					if (this._textBox.Text == oldText) {
						return;
					}

					// 対象文字列更新
					this.UpdateCurrentText();

					if (string.IsNullOrEmpty(this.CurrentText)) {
						this.SuggestClose();
						return;
					}

					// AutoModeであれば再フィルタリング
					this._collectionView?.Refresh();

					if (this._suggest.Items.Count == 0) {
						this.SuggestClose();
						return;
					}

					if (!this._popup.IsOpen) {
						this.SuggestOpen();
					}
				}));
		}

		/// <summary>
		/// サジェストを開く
		/// </summary>
		private void SuggestOpen() {
			this._popup.PlacementTarget = this._textBox;
			this._popup.PlacementRectangle = this._textBox.GetRectFromCharacterIndex(this._textBox.CaretIndex);
			this._popup.IsOpen = true;
		}

		/// <summary>
		/// サジェストを閉じる
		/// </summary>
		private void SuggestClose() {
			this._popup.IsOpen = false;
		}

		/// <summary>
		/// 対象文字列更新
		/// </summary>
		private void UpdateCurrentText() {
			if (this._textBox.CaretIndex <= 0) {
				this.CurrentText = "";
				return;
			}

			var start = this._textBox.Text.LastIndexOfAny(this.Separator.ToArray(), this._textBox.CaretIndex - 1) + 1;
			var end = this._textBox.Text.IndexOfAny(this.Separator.ToArray(), this._textBox.CaretIndex - 1) + 1;
			if (end == 0) {
				end = this._textBox.Text.Length;
			}

			if (start == end) {
				this.CurrentText = "";
				return;
			}
			var text = this._textBox.Text.Substring(start, end - start);
			foreach (var c in this.Separator) {
				text = text.Replace(c.ToString(), "");
			}
			this.CurrentText = text;
		}

		/// <summary>
		/// AutoMode時のフィルター関数
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private bool Filter(object args) {
			if (!(args is ICandidate c)) {
				return false;
			}
			if (this.CurrentText == null) {
				return false;
			}
			if (this.IgnoreCase) {
				return c.Word.ToLower().Contains(this.CurrentText.ToLower());
			}
			return c.Word.Contains(this.CurrentText);
		}

		/// <summary>
		/// 決定
		/// </summary>
		private void DecideWord() {
			if (this._suggest.SelectedValue == null) {
				return;
			}

			if (!(this._suggest.SelectedValue is ICandidate candidate)) {
				return;
			}

			var start = this._textBox.Text.LastIndexOfAny(this.Separator.ToArray(), this._textBox.CaretIndex - 1) + 1;

			// 決定したワードに置換する
			this._textBox.Text =
				this._textBox.Text
					.Remove(start, this.CurrentText.Length)
					.Insert(start, candidate.Word);
			this._textBox.CaretIndex = start + candidate.Word.Length;

			this.SuggestClose();
			this._textBox.Focus();
		}
	}
}
