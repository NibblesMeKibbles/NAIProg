using Godot;

namespace NAIProg;
public partial class FullScreen_Button : Button {
	public override void _Ready() {
		ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.ExclusiveFullscreen;
	}

	public override void _Toggled(bool buttonPressed) {
		DisplayServer.WindowSetMode(ButtonPressed ? DisplayServer.WindowMode.ExclusiveFullscreen : DisplayServer.WindowMode.Windowed);
	}
}