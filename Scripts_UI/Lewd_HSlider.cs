using Godot;

namespace NAIProg;
public partial class Lewd_HSlider : HSlider {
	public override void _ValueChanged(double newValue) {
		Game.Instance.ImageGen.OnLewdSliderUpdate(newValue / MaxValue);
	}
}