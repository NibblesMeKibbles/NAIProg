using Godot;

namespace NAIProg;
public partial class Pause_Button : Button {
	public override void _Toggled(bool state) {
		Game.Instance.ImageGen.LewdActive = !state;
	}
}