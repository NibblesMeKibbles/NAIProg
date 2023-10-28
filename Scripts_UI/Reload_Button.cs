using Godot;

namespace NAIProg;
public partial class Reload_Button : Button {
	public override void _Pressed() {
		Game.Instance.LoadConfigJson();
	}
}