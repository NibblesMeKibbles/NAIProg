using Godot;
using Godot.Collections;

namespace NAIProg;
public partial class Game : Node2D {
	public static Game Instance { get; private set; }
	public Dictionary Config;
	public Prompt Prompt;
	public ImageGen ImageGen;
	public System.Collections.Generic.Dictionary<string, Node> UI = new();

	public const string resConfigPath = "res://config.json";
	public const string resReadmePath = "res://README.md";
	public const string userRootPath = "user://";
	public const string userConfigPath = userRootPath + "config.json";
	public const string userReadmePath = userRootPath + "README.md";
	public const string imagesRootPath = userRootPath + "SavedImages/";
	public const string ApiTest = "https://api.novelai.net/user/information";
	public const string ApiImage = "https://api.novelai.net/ai/generate-image";

	private bool firstConfigLoad = true;

	public override void _Ready() {
		Instance = this;

		// Build UI lookup Dictionary
		Array<Node> allNodes = FindChildren("*");
		foreach (Node node in allNodes) {
			UI[node.Name] = node;
		}

		Prompt = new(this);
		ImageGen = new(this);
		LoadConfigJson();
	}

	public override void _Process(double delta) {
		ImageGen.Process(delta);
	}

	public static string GetUserConfigProperty(string pattern) {
		if (!FileAccess.FileExists(userConfigPath)) {
			return null;
		}
		FileAccess file = FileAccess.Open(userConfigPath, FileAccess.ModeFlags.Read);
		string content = file.GetAsText();
		file.Close();
		file.Dispose();

		RegEx reg = RegEx.CreateFromString(pattern);
		RegExMatch match = reg.Search(content);
		if (match != null) {
			return match.Strings[0];
		}
		return null;
	}

	public void LoadConfigJson(bool forceReset = false) {
		bool resetedApiKey = false;
		if (Engine.IsEditorHint() || OS.HasFeature("editor") || !FileAccess.FileExists(userConfigPath) && FileAccess.FileExists(resConfigPath) || forceReset) {
			// Save the user config ApiKey and AutoHideMenu values
			string oldApiKey = GetUserConfigProperty("pst-[^\"]+");
			string oldAutoHideMenu = GetUserConfigProperty("\"AutoHideMenu\": (false|true),");

			// Copy config.json and README.md
			CopyResFileToUser(resConfigPath, userConfigPath);
			CopyResFileToUser(resReadmePath, userReadmePath);

			// Reset config.json to defaults
			FileAccess fileUser = FileAccess.Open(userConfigPath, FileAccess.ModeFlags.Write);
			FileAccess fileRes = FileAccess.Open(resConfigPath, FileAccess.ModeFlags.Read);
			string content = fileRes.GetAsText();

			// but preserve the old ApiKey and AutoHideMenu values
			if (oldApiKey != null) {
				RegEx reg = RegEx.CreateFromString("pst-[^\"]+");
				content = reg.Sub(content, oldApiKey);
			}
			if (oldAutoHideMenu != null) {
				RegEx reg = RegEx.CreateFromString("\"AutoHideMenu\": (false|true),");
				content = reg.Sub(content, oldAutoHideMenu);
			}

			fileUser.StoreString(content);
			fileUser.Close();
			fileRes.Close();
			resetedApiKey = oldApiKey == null || oldApiKey.Contains("******");
		}
		// Parse JSON
		if (FileAccess.FileExists(userConfigPath)) {
			FileAccess file = FileAccess.Open(userConfigPath, FileAccess.ModeFlags.Read);
			string contents = file.GetAsText();
			if (!string.IsNullOrWhiteSpace(contents)) {
				Dictionary json = Json.ParseString(contents).AsGodotDictionary();
				if (json.ContainsKey("LewdOffsetRandomness")) {
					Config = json;
					LoadData(!resetedApiKey);
				}
			}
			file.Close();
		}
	}

	private void CopyResFileToUser(string pathRes, string pathUser) {
		FileAccess fileRes = FileAccess.Open(pathRes, FileAccess.ModeFlags.Read);
		if (fileRes == null) {
			CreateAlert("Failed to open " + pathRes);
			return;
		}
		FileAccess fileUser = FileAccess.Open(pathUser, FileAccess.ModeFlags.Write);
		if (fileRes == null) {
			CreateAlert("Failed to create " + pathUser +
				"\nFile Path: " + ProjectSettings.GlobalizePath(pathUser));
			fileRes.Close();
			return;
		}
		fileUser.StoreBuffer(fileRes.GetBuffer((long)fileRes.GetLength()));
		fileRes.Close();
		fileUser.Close();
	}

	public void OpenConfigJson() {
		Error error = OS.ShellOpen(ProjectSettings.GlobalizePath(userRootPath));
		if (error != Error.Ok) {
			CreateAlert("Open shell window failed" +
				"\nError Code: " + error.ToString());
		}
	}

	public void LoadData(bool testApi = true) {
		ImageGen.Goal = ImageGen.Random.RandfRange((float)Config["GoalMin"], (float)Config["GoalMax"]) * 60.0;
		((Button)UI["AutoHide_Button"]).ButtonPressed = (bool)Config["AutoHideMenu"];
		ImageGen.UpdateLewdSlider();

		if (testApi) {
			ImageGen.TestAPI();
		}
		else {
			((Label)UI["Loading_Label"]).Visible = false;
			((Window)UI["ApiKey_Window"]).Show();
		}
		if (firstConfigLoad) {
			firstConfigLoad = false;
		}
		else if (testApi) {
			CreatePopup("Successfully reloaded JSON.");
		}
	}

	public void EnableControls() {
		((Button)UI["Pause_Button"]).Disabled = false;
		((HSlider)UI["Lewd_HSlider"]).Editable = true;
	}

	public void CreateAlert(string message) {
		AcceptDialog acceptDialog = new();
		((CanvasLayer)UI["CanvasLayer"]).AddChild(acceptDialog);
		acceptDialog.Title = "";
		acceptDialog.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen;
		acceptDialog.DialogText = message;
		acceptDialog.Show();
	}
	public void CreatePopup(string message) {
		PopupMenu popup = new();
		((CanvasLayer)UI["CanvasLayer"]).AddChild(popup);
		popup.Title = "";
		popup.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen;
		popup.AddItem(message);
		popup.Show();
	}
}
