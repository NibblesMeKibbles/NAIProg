using Godot;

namespace NAIProg;
public partial class Prompt_Button : Button {
	public override void _Pressed() {
		((Window)Game.Instance.UI["Prompt_Window"]).Show();
	}
}