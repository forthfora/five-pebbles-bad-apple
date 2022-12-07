# Install
- Download and install BOI / BepInEx: https://rainworldmodding.miraheze.org/wiki/BOI#Installation
- Download the most recent .dll from the releases page and place it in the /Rain World/Mods folder!
- Alternatively, just drop the .dll into the BOI interface
- Don't forget to enable the mod through BOI first!

Dependencies:
- EnumExtender.dll (this should come with BOI's automatic BepInEx installation!)

# Credits
As the first mod I have ever made for Rain World, you are legally not allowed to judge this code!

Seriously though, it's not pretty, but huge thanks to those who helped me on the RW Discord, especially Slime_Cubed, Bro and Wack for answering my questions and showing me how to play audio!

Woultkolkman's FivePebblesPong mod was used as a reference for a large part of this mod, so huge thanks to them too!

Please check out their mod, which I think is much cleaner and more technically impressive than this one:
https://github.com/woutkolkman/fivepebblespong

# Usage
On Survivor / Monk, the pearl will be in Pebbles' chamber on entering, so the video sequence will start playing immediately!
This also means Pebbles will always kill you... 

On Hunter, the pearl will be in Hunter's stomach from the start - the video will begin playing when you enter the chamber with the pearl in hand!

BEWARE! There might be a memory leak issue with the audio player, so playing the video more than 2 times after launching the game may or may not crash the game with an 'out of memory' exception! Sometimes it's fine, sometimes it's not!

Overall, just don't expect this mod to be 100% stable, at all!

# Custom Video

If you want to add your own video and sound, you need to first convert the video into individual png images, one for each frame (you could do this with something like ffmpeg)!

You can download videos off of Youtube with a tool such as yt-dlp!

Make sure the frames are not too large! The game will crash otherwise! The videos I have tested with were 480x360 - after this, you just replace all the frames in the frames folder with your frames!

Make sure that all the frame pngs are in alphabetical order by name, so that the video plays in the correct order!

Also, the video you use should be 30 fps - you can change the framerate if you modify the constant in the Hooks script and compile the dll yourself, but the performance would probably suffer at higher values!

For sound, you need a wav file of the videos audio (ffmpeg can extract the audio from a video file), and just replace the wav file in the Sounds directory with your audio file - just make sure it is named BadAppleMusic.wav!

