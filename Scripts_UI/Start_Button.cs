using Godot;

namespace NAIProg;
public partial class Start_Button : Button {
	public override void _Pressed() {
		Game.Instance.ImageGen.LewdActive = true;
		Game.Instance.EnableControls();
		Visible = false;
	}
}