using Godot;

namespace NAIProg;
public partial class Save_Button : Button {
	public override void _Pressed() {
		Game.Instance.ImageGen.SaveImage();
	}
}