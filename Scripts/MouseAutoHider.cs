using Godot;

namespace NAIProg;
public partial class MouseAutoHider : Node2D {
	private double mouseHideTimer = 3.0;

	public override void _Process(double delta) {
		if (Input.GetLastMouseVelocity() == Vector2.Zero) {
			mouseHideTimer -= delta;
			if (mouseHideTimer <= 0 && Input.MouseMode == Input.MouseModeEnum.Visible) {
				Input.MouseMode = Input.MouseModeEnum.Hidden;
			}
		}
		else {
			mouseHideTimer = 3.0;
			if (Input.MouseMode == Input.MouseModeEnum.Hidden) {
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
		}
	}
}