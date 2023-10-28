using Godot;

namespace NAIProg;
public partial class Reset_ConfirmationDialog : ConfirmationDialog {
	public override void _Ready() {
		GetOkButton().Pressed += CallResetJson;
	}
	private void CallResetJson() {
		Game.Instance.LoadConfigJson(true);
	}
}