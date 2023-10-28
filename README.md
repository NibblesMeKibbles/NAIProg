# NAI Prog
Short for **N**ovel**AI** Diffusion **Prog**ressively More Lewd Automated Image Generator, the name speaks for itself. If you're familiar with coding and prompting, viewing the [`config.json`](config.json) self-explains most of the [Functionality](#functionality).

## In-Game Buttons
|||
|-|-|
|Start|If the test API call to NovelAI API succeeds, click to start the image generation timer and Lewd progression|
|||
|Last Image|View the previously loaded image|
|Pause|Pause or unpause the image generation timer and Lewd progression|
|Next Image|View the next image or generate a new image if already viewing the latest|
|Lewd Slider|View and update the current [Lewd value](#lewd-value)|
|||
|Save Current Image (with metadata)|Save the currently viewed image to a timestamped `.png` file with full NovelAI metadata|
|Reload config.json|Re-import the user folder `config.json` and its configurations|
|Reset config.json to default|Restore the user `config.json` to the default resource `config.json` contents|
|Open user directory|Open a shell window to the user directory that contains the local `config.json`, `README.md`, and any `/SavedImages/`|
|Open Prompt & Undesired Content Window|Open a non-editable window that displays the currently viewed image's Prompt and Undesired Content|
|Toggle Menu AutoHide|When AutoHide is enabled, the bottom menu will only be visible when the mouse is hovering over it|
|Toggle FullScreen Mode|Switch between Windows and FullScreen display mode|
|Exit to Desktop|Quit the game|

# Functionality
Going forward, the code tag (`example text`) is heavily used when referencing attributes present in the [`config.json`](config.json) file.

## API Payload
The base API payload is visible at the bottom of the [`config.json`](config.json) file. To assuredly modify values (such as `model` name): open the NovelAI image generator website in the browser, select your preferred image and model settings, open the Developer Tools (F12?), open the Network tab, and generate an image using normal means. The Network tab will add a line with the name `/api/generate-image` (or just `generate-image`), select that API call, select the `Payload` tab, and copy desired values to the `config.json`.

Do not modify the `%INPUT_PROMPT%`, `%NEGATIVE_PROMPT%` and `seed` values as these 3 are dynamically updated during each image generation, explanation provided below.

## Lewd value
The **Lewd** value is a number that increases from 0 -> 1 over the span of the **goal** time. The **goal** time is a random value between `GoalMin` and `GoalMax` (in minutes).
When a random **Lewd** value offset is being rolled, how much it can deviate is multiplied by the `LewdOffsetRandomness`, meaning that increasing this value can result in wide swings of lewdness or decreasing it can result in a more consistent escalation from beginning to end.

## Selecting Prompt and Undesired Content
The following is the functionality used when constructing the dynamic values of the API payload for image generation:
* Select a random `Characters` (with equal probability).
* Generate the **Prompt**: `Quality`, `Style`, selected `Characters`'s `Prompt`, random `Mood`, random `Scenery`, and lastly `Pose` + `Outfit` + `OutfitMod` `Prompt`s with each being selected based on the current **Lewd** value, with an offset.
* Generate the **Undesired Content**: `UndesiredContentPreset`, selected `Characters`'s `UndesiredContent`, and `UndesiredContent`.
* Select a random **seed** number.

Modifying the `Quality` and `UndesiredContentPreset` attributes is the same as turning off the toggle in the NovelAI browser for "Add Quality Tags" and setting "Undesired Content Preset" to None (the `config.json` base payload attributes for `ucPreset` and `qualityToggle` do **NOT** inject these tags when using raw API calls like from this game/app).

`Style` is a string of tags consistent across all image generations, typically for art style, medium, and basic composition.

`UndesiredContent` is static across all image generations, meaning that it should contain tags that are all encompassing across all `Pose`s and `Outfit`s. NAID V2 is significantly better about **Prompt** accuracy in the positive, allowing a static `UndesiredContent` to provide sufficient results.

A random `Mood`, `Scenery`, `Pose`, and `Outfit` is added to the **Prompt** each image generation.
The `Pose`, `Outfit`, and `OutfitMod` uses the **Lewd** value to bias which value to select. Each one rolls a *unique* random offset from the current **Lewd** value to increase variety.

The "Btm", "Top", and "Non" `Type`s are for compatibility (abbr. for Bottom, Top, and None). Examples: a `Pose` for "shirt pull" should `Req`uire a "Top" `Outfit` to be selected. A `Pose` involving partial nudity should have the respective portion `Ban`ned, or even `Req` = "Non" if full nudity is required.

When the AI is provided a basic **Prompt** of "jeans", typically it will generate a shirt to accompany it. To influence a progression of lewdness, the `OutfitMod` is used to provoke a progression towards "bottomless" or "topless". If a "Top" `Outfit` is selected, then the "Top" `Outfit` mod will influence towards less bottom clothes, and vice versa.

Bonus functionality: when a `Pose` is selected that contains `Ban`, then `OutfitMod` will always pick the highest `Lewd` option. For example, if a `Pose` `Ban`s "Btm" due to a sexual act where bottom clothes will interfere with generation, then the `Outfit` is most likely to be a "Top", thus the `OutfitMod` category will also be "Top". Since a `Pose` `Ban` is present, it will select the highest index `OutfitMod` "Top", which happens to contain the most detailed tags of the characters bottom half and assist the **Prompt** with the sexual acts image generation reliability.
