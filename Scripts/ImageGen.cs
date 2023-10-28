using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAIProg;
public partial class ImageGen {

	private readonly Game Game;
	private Prompt Prompt => Game.Prompt;

	public bool LewdActive = false;
	public double Time = 0.0;
	public double Goal = 600.0;
	public double Interval => (double)Game.Config["Interval"];
	public double Lewd => Time / Goal;
	public double LastImage = 0.0;
	public int ImageIndex = 0;
	public List<byte[]> Images = new();
	public List<string> Prompts = new();
	public List<string> Undesireds = new();

	public RandomNumberGenerator Random = new();

	private readonly HttpRequest httpRequestTest;
	private readonly HttpRequest httpRequestImage;
	// The Prompt sent to the in-progress API call. Promoted to List<>Prompts variable when successully imaged
	private string pendingPrompt;
	private string pendingUndesired;

	private bool testApiPending = false;
	private bool imageGenPending = false;

	public ImageGen(Game game) {
		Game = game;

		httpRequestTest = (HttpRequest)Game.UI["HTTPRequest_Test"];
		httpRequestTest.RequestCompleted += OnTestRequestCompleted;
		httpRequestTest.Timeout = 15.0;

		httpRequestImage = (HttpRequest)Game.UI["HTTPRequest_Image"];
		httpRequestImage.DownloadFile = "user://images.zip";
		httpRequestImage.RequestCompleted += OnImageRequestCompleted;
		httpRequestImage.Timeout = 25.0;
	}

	public void Process(double delta) {
		if (LewdActive) {
			Time += delta;
			UpdateLewdSlider();
			if (!imageGenPending && (Time > LastImage + Interval || LastImage == 0.0)) {
				imageGenPending = true;
				GenerateImage();
			}
		}
	}

	public void TestAPI() {
		if (testApiPending) {
			return;
		}
		testApiPending = true;
		((Label)Game.UI["Loading_Label"]).Visible = true;
		string[] headers = new string[] {
			"Authorization: Bearer " + Game.Config["ApiKey"]
		};
		httpRequestTest.Request(Game.ApiTest, headers);
	}
	private void OnTestRequestCompleted(long result, long responseCode, string[] headers, byte[] body) {
		GD.Print("complete");
		if (result != (long)HttpRequest.Result.Success || responseCode != 200) {
			string responseBody = Encoding.UTF8.GetString(body);
			Game.CreateAlert("Test API Call" +
				"\nHttpRequest Status: " + ((HttpRequest.Result)result).ToString() +
				"\nResponse Code: " + responseCode.ToString() +
				"\nResponse Body: " + responseBody);
		}
		else {
			((Button)Game.UI["Start_Button"]).Disabled = false;
			((Button)Game.UI["Start_Button"]).TooltipText = "Start\n( Shortcuts: Enter )";
		}
		((Label)Game.UI["Loading_Label"]).Visible = false;
		testApiPending = false;
	}

	public void GenerateImage() {
		Random.Randomize();
		((Button)Game.UI["Next_Button"]).Disabled = true;
		((Label)Game.UI["Loading_Label"]).Visible = true;

		string[] headers = new string[] {
			"Authorization: Bearer " + Game.Config["ApiKey"],
			"Content-Type: application/json"
		};

		Dictionary character = Prompt.GetCharacter();
		pendingPrompt = Prompt.GetPrompt(character);
		// Undesired Content: Preset + Character + Other
		pendingUndesired = ((string[])Game.Config["UndesiredContentPreset"]).Concat((string[])character["UndesiredContent"]).Concat((string[])Game.Config["UndesiredContent"]).ToArray().Join(", ");

		((Dictionary)Game.Config["ApiPayloadBody"])["input"] = pendingPrompt;
		((Dictionary)((Dictionary)Game.Config["ApiPayloadBody"])["parameters"])["negative_prompt"] = pendingUndesired;
		((Dictionary)((Dictionary)Game.Config["ApiPayloadBody"])["parameters"])["seed"] = Random.Randi();
		string body = Json.Stringify((Dictionary)Game.Config["ApiPayloadBody"]);

		Error error = httpRequestImage.Request(Game.ApiImage, headers, HttpClient.Method.Post, body);
		if (error != Error.Ok) {
			Game.CreateAlert("Failed to send HttpRequest" +
				"\nURI: /api/generate-iamge" +
				"\nError Code: " + error.ToString());
			SetLewdActive(false);
			((Label)Game.UI["Loading_Label"]).Visible = false;
		}

		LastImage = Time;
	}

	// HttpRequest byte stream is in x-zip-compress format, save to .zip, then unzip the .png
	private void OnImageRequestCompleted(long result, long responseCode, string[] headers, byte[] body) {
		if (result != (long)HttpRequest.Result.Success || responseCode != 200) {
			string responseBody = Encoding.UTF8.GetString(body);
			if (FileAccess.FileExists("user://images.zip")) {
				FileAccess fileAccess = FileAccess.Open("user://images.zip", FileAccess.ModeFlags.Read);
				if (fileAccess.GetLength() < 1000) {
					responseBody = fileAccess.GetAsText();
				}
			}
			Game.CreateAlert("/api/generate-image" +
				"\nHttpRequest Status: " + ((HttpRequest.Result)result).ToString() +
				"\nResponse Code: " + responseCode.ToString() +
				"\nResponse Body: " + responseBody);
			SetLewdActive(false);
			((Label)Game.UI["Loading_Label"]).Visible = false;

			if (responseCode == 500) {
				((Button)Game.UI["Next_Button"]).Disabled = false;
			}
		}
		else {
			ZipReader reader = new();
			Error error = reader.Open("user://images.zip");
			if (error != Error.Ok) {
				Game.CreateAlert("Unzip image generation download failed" +
					"\nError Code: " + error.ToString());
				SetLewdActive(false);
			}
			else {
				byte[] imageBytes = reader.ReadFile("image_0.png");
				if (imageBytes == null || imageBytes.Length == 0) {
					Game.CreateAlert("Unzipped PNG file read failed" +
						"\nError Code: " + error.ToString());
					SetLewdActive(false);
				}
				else {
					Images.Add(imageBytes);
					Prompts.Add(pendingPrompt);
					Undesireds.Add(pendingUndesired);
					ImageIndex = Images.Count - 1;
					LoadImage();
				}
			}
		}
	}

	public void BackImage() {
		ImageIndex = Mathf.Max(ImageIndex - 1, 0);
		if (ImageIndex == 0) {
			((Button)Game.UI["Back_Button"]).Disabled = true;
		}
		LoadImage();
		SetLewdActive(false);
	}
	public void NextImage() {
		if (ImageIndex >= Images.Count - 1) {
			GenerateImage();
		}
		else {
			ImageIndex = Mathf.Min(ImageIndex + 1, Images.Count - 1);
			if (ImageIndex >= Images.Count - 1) {
				SetLewdActive(true);
			}
			LoadImage();
		}
	}

	public void LoadImage() {
		var image = new Image();
		Error error = image.LoadPngFromBuffer(Images[ImageIndex]);
		if (error != Error.Ok) {
			Game.CreateAlert("Load byte buffer to PNG format failed" +
				"\nError Code: " + error.ToString());
			Images.RemoveAt(ImageIndex);
			Prompts.RemoveAt(ImageIndex);
			Undesireds.RemoveAt(ImageIndex);
			ImageIndex--;
			SetLewdActive(false);
		}
		else {
			var texture = ImageTexture.CreateFromImage(image);
			if (texture == null) {
				Game.CreateAlert("Convert loaded PNG to texture failed" +
					"\nError Code: " + error.ToString());
				Images.RemoveAt(ImageIndex);
				Prompts.RemoveAt(ImageIndex);
				Undesireds.RemoveAt(ImageIndex);
				ImageIndex--;
				SetLewdActive(false);
			}
			else {
				((TextureRect)Game.UI["Image_TextureRect"]).Texture = texture;

				((TextEdit)Game.UI["Prompt_TextEdit"]).Text = Prompts[ImageIndex];
				((TextEdit)Game.UI["Undesired_TextEdit"]).Text = Undesireds[ImageIndex];

				((Button)Game.UI["Back_Button"]).Disabled = false;
				((Button)Game.UI["Next_Button"]).Disabled = false;
				imageGenPending = false;
			}
		}
		((Label)Game.UI["Loading_Label"]).Visible = false;
	}
	public void SaveImage() {
		if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(Game.imagesRootPath))) {
			Error error = DirAccess.MakeDirAbsolute(ProjectSettings.GlobalizePath(Game.imagesRootPath));
			if (error != Error.Ok) {
				Game.CreateAlert("Failed to create /SavedImages/ folder" +
					"\nFolder Path: " + ProjectSettings.GlobalizePath(Game.imagesRootPath) +
					"\nError Code: " + error.ToString());
				return;
			}
		}
		string pngName = Godot.Time.GetDatetimeStringFromSystem().Replace(':', '-').Replace('T', '-') + ".png";
		string filePath = Game.imagesRootPath + "Image_" + pngName;
		FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
		if (file == null) {
			Game.CreateAlert("Failed to create destination file" +
				"\nImage Index: " + ImageIndex.ToString() +
				"\nFile Path: " + filePath);
			return;
		}
		if (Images[ImageIndex] == null) {
			Game.CreateAlert("Image bytes is empty" +
				"\nImage Index: " + ImageIndex.ToString() +
				"\nFile Path: " + filePath);
			file.Close();
			return;
		}
		file.StoreBuffer(Images[ImageIndex]);
		file.Close();
		Game.CreatePopup("Succesfully saved image.\nFile Path: " + pngName);
	}

	public void SetLewdActive(bool state) {
		LewdActive = state;
		((Button)Game.UI["Pause_Button"]).SetPressedNoSignal(!LewdActive);
		imageGenPending = false;
	}

	public void UpdateLewdSlider() {
		((HSlider)Game.UI["Lewd_HSlider"]).SetValueNoSignal(100.0 * Time / Goal);
	}
	public void OnLewdSliderUpdate(double sliderPercent) {
		double delta = Time % Interval;
		Time = Goal * sliderPercent;
		LastImage = Time - delta;
	}
}