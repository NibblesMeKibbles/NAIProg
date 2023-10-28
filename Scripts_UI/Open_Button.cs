using Godot;

namespace NAIProg;
public partial class Open_Button : Button {
	public override void _Pressed() {
		Game.Instance.OpenConfigJson();
	}
}