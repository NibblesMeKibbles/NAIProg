using Godot;

namespace NAIProg;
public partial class Next_Button : Button {
	public override void _Pressed() {
		Game.Instance.ImageGen.NextImage();
	}
}