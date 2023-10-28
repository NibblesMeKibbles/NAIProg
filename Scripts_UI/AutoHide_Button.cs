using Godot;

namespace NAIProg;
public partial class AutoHide_Button : Button {
	public override void _Pressed() {
		Game.Instance.Config["AutoHideMenu"] = ButtonPressed;
	}
}