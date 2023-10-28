using Godot;

namespace NAIProg;
public partial class Bar_MarginContainer : MarginContainer {
	public override void _Ready() {
		MouseEntered += OnMouseEnterMenu;
		MouseExited += OnMouseExitMenu;
	}

	public void OnMouseEnterMenu() {
		GetNode<VBoxContainer>("Bar_VBoxContainer").Modulate = new Color(1f, 1f, 1f, 1f);
	}
	public void OnMouseExitMenu() {
		if ((bool)Game.Instance.Config["AutoHideMenu"]) {
			GetNode<VBoxContainer>("Bar_VBoxContainer").Modulate = new Color(0f, 0f, 0f, 0f);
		}
	}
}