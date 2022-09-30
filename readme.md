# WeirdViewer

### What is this tool?
WeirdViewer is a tool to view .wimpy files from [Return to Monkey Island](https://www.returntomonkeyisland.com).

At the moment it supports:
- View background layers
- View the main layer with all non-animated objects
- View hotspots
- Visualize walkboxes
- Visualize lights
- Visualize emitters

What it can't do (yet)
- Show objects with skeletal animations. Some parts of the scenes are animated using .anim files which describe a polygonal mesh which has a texture mapped onto it. This mesh is then animated by the game.
- Show any details about the scene's objects
- Modify / save / author new .wimpy files

### How to use

Call it like `weirdViewer.exe <Path_to_monkey_island> <filename>.weird`. 

The program will then open all of the .ggpack??-Files using keys extracted from the game's executable ("Return to Monkey Island.exe" in the game's directory).

It will open the .wimpy file from the pack and load the referenced sprite-sheets. This may take a few seconds, depending on how many there are.
Note: It will always load the highest available resolution and I haven't implemented scaling of the scene yet, so I recommend viewing files on a high-resolution monitor.

It will open a window and show the scene.
The panel on the left allows you to toggle individual layers on and off.
Scrolling on the right side will move the scene left and right. The foreground / background layers will move as dictated by their parallax settings.

### Acknowledgements:

- If you would like to explore the game's other files, use the [Thimbleweed Park Explorer](https://github.com/bgbennyboy/Thimbleweed-Park-Explorer). Some of the code used in this project was written for this project first.

Packages used:
- Newtonsoft.json
- [BCnEncoder.Net](https://github.com/Nominom/BCnEncoder.NET)
