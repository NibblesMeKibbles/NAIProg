using Godot;

namespace NAIProg;
public partial class Prompt_Window : Window {
	public override void _Ready() {
		CloseRequested += OnCloseRequested;
	}
	private void OnCloseRequested() {
		Hide();
	}
}