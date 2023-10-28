using Godot;

namespace NAIProg;
public partial class Exit_Button : Button {
	public override void _Pressed() {
		GetTree().Quit();
	}
}