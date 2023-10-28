using Godot;

namespace NAIProg;
public partial class ApiKey_Window : Window {
	public override void _Ready() {
		GetNode<Button>("ApiKey_Button").Pressed += OnSubmitApiKey;
		CloseRequested += OnCloseRequested;
	}

	public void OnSubmitApiKey() {
		UpdateApiKey(GetNode<TextEdit>("ApiKey_TextEdit").Text);
		Game.Instance.LoadConfigJson();
		OnCloseRequested();
	}

	private static void UpdateApiKey(string newApiKey) {
		FileAccess file = FileAccess.Open(Game.userConfigPath, FileAccess.ModeFlags.Read);
		string content = file.GetAsText();
		file.Close();
		file.Dispose();
		file = FileAccess.Open(Game.userConfigPath, FileAccess.ModeFlags.Write);
		RegEx reg = RegEx.CreateFromString("pst-[^\"]+");
		content = reg.Sub(content, newApiKey);
		file.StoreString(content);
		file.Close();
	}

	private void OnCloseRequested() {
		Hide();
	}
}