using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAIProg;
public partial class Prompt {

	private readonly Game Game;
	private ImageGen ImageGen => Game.ImageGen;
	private double LewdOffsetRandomness => (double)Game.Config["LewdOffsetRandomness"];

	public Prompt(Game game) {
		Game = game;
	}

	public string GetPrompt(Dictionary character) {
		string[] moods = (string[])Game.Config["Mood"];

		Dictionary pose = GetPose();
		Dictionary outfit = GetOutfit(pose);
		Dictionary outfitMod = GetOutfitMod((string)pose["Ban"], (string)outfit["Type"]);

		string[] prompt = new string[] {
			(string)Game.Config["Quality"],
			(string)Game.Config["Style"],
			((string[])character["Prompt"]).Join(", "),
			moods[ImageGen.Random.RandiRange(0, moods.Length - 1)],
			GetScenery(),
			(string)pose["Prompt"],
			(string)outfit["Prompt"],
			outfitMod != null ? (string)outfitMod["Prompt"] : ""
		};
		return prompt.Join(",    ");
	}

	private float GetRandomRange(float min, float max) {
		return ImageGen.Random.Randf() * (max - min) + min;
	}

	// Get current Lewd percentile and add an offset (with a ratio'd second offset to weakly central bias)
	public float GetLewdWithOffset(float lower, float upper) {
		float ratio = 3f;
		float offset1 = GetRandomRange(lower * (float)LewdOffsetRandomness, upper * (float)LewdOffsetRandomness) * (ratio - 1f);
		float offset2 = GetRandomRange(lower * (float)LewdOffsetRandomness, upper * (float)LewdOffsetRandomness);
		return Mathf.Clamp((float)ImageGen.Lewd + (offset1 + offset2) / ratio, 0f, 1f);
	}

	// Higher index has highter probability
	public int GetPowWeightedIndex(double pow, int maxIndex) {
		float rand = GetRandomRange(0f, (float)Mathf.Pow(maxIndex, pow));
		return Mathf.RoundToInt(Mathf.Pow(rand, 1f/(pow)));
	}

	public Dictionary GetCharacter() {
		Array characters = (Array)Game.Config["Character"];
		return (Dictionary)characters[ImageGen.Random.RandiRange(0, characters.Count - 1)];
	}

	public string GetScenery() {
		Array sceneries = (Array)Game.Config["Scenery"];
		Dictionary scenery = (Dictionary)sceneries[ImageGen.Random.RandiRange(0, sceneries.Count - 1)];
		string[] variants = (string[])scenery["Variants"];
		int index = ImageGen.Random.RandiRange(0, variants.Length - 1);
		return (string)scenery["Prompt"] + (!string.IsNullOrWhiteSpace(variants[index]) ? ", " : "") + variants[index];
	}

	public Dictionary GetPose() {
		float lewd = GetLewdWithOffset(-0.2f, 0.3f);
		Array poses = (Array)Game.Config["Pose"];
        List<Dictionary> poseVariants = new();
		foreach (Dictionary pose in poses.Select(p => (Dictionary)p)) {
			Array variants = (Array)pose["Variants"];
			foreach (Dictionary variant in variants.Select(v => (Dictionary)v)) {
				if ((float)pose["Lewd"] + (float)variant["Lewd"] <= lewd) {
					poseVariants.Add(new Dictionary() {
						{ "Lewd", (float)pose["Lewd"] + (float)variant["Lewd"] },
						{ "Ban", (string)pose["Ban"] },
						{ "Req", (string)pose["Req"] },
						{ "Prompt", "{" + (string)pose["Prompt"] + (!string.IsNullOrWhiteSpace((string)variant["Prompt"]) ? ", " : "") + (string)variant["Prompt"] + "}" }
					});
				}
			}
		}
		poseVariants = poseVariants.OrderBy(pV => (float)pV["Lewd"]).ToList();
		return poseVariants[GetPowWeightedIndex(2.0 + ImageGen.Lewd, poseVariants.Count - 1)];
	}

	public Dictionary GetOutfit(Dictionary pose) {
		string poseReq = (string)pose["Req"];
		string poseBan = (string)pose["Ban"];

		float lewd = GetLewdWithOffset(-0.2f, 0.3f);
		Array outfits = (Array)Game.Config["Outfit"];
		Variant[] options = outfits.Where(p =>
			(float)((Dictionary)p)["Lewd"] <= lewd &&
			(string.IsNullOrWhiteSpace(poseReq) || (string)((Dictionary)p)["Type"] == poseReq) &&
			(string.IsNullOrWhiteSpace(poseBan) || (string)((Dictionary)p)["Type"] != poseBan)
		).ToArray();

		return (Dictionary)options[GetPowWeightedIndex(2.0 + ImageGen.Lewd, options.Length - 1)];
	}

	public Dictionary GetOutfitMod(string poseBan, string outfitType) {
		if (outfitType != "Top" && outfitType != "Btm" && outfitType != "Non") {
			return null;
		}
		float offset = GetLewdWithOffset(-0.3f, 0.3f);
		Array mods = (Array)((Dictionary)Game.Config["OutfitMod"])[outfitType];
		// If selected Pose has a ban, then it needs details (thus always max Lewd)
		if (!string.IsNullOrWhiteSpace(poseBan)) {
			return (Dictionary)mods[^1];
		}
		for (int i = mods.Count - 1; i >= 0; i--) {
			Dictionary mod = (Dictionary)mods[i];
			if ((float)mod["Lewd"] < offset) {
				return mod;
			}
		}
		return null;
	}
}