using Godot;

namespace NAIProg;
public partial class Back_Button : Button {
	public override void _Pressed() {
		Game.Instance.ImageGen.BackImage();
	}
}