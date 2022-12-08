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

woultkolkman's FivePebblesPong mod was used as a reference for a large part of this mod, so huge thanks to them too!

Please check out their mod here: https://github.com/woutkolkman/fivepebblespong

# Usage
On Survivor / Monk, the pearl will be in Pebbles' chamber on entering, so the video sequence will start playing immediately!
This also means Pebbles will always kill you... 

On Hunter, the pearl will be in Hunter's stomach from the start - the video will begin playing when you enter the chamber with the pearl in hand!

BEWARE! There might be a memory leak issue with the audio player, so playing the video more than 2 times after launching the game may or may not crash the game with an 'out of memory' exception! Sometimes it's fine, sometimes it's not!

Overall, just don't expect this mod to be 100% stable, at all!

# Adding a Custom Video

This guide is intended for Windows! It may work on other OSes, but the process will likely be different!

- yt-dlp is very useful for downloading videos off of YouTube, you can get it from the releases section here: https://github.com/yt-dlp/yt-dlp

- ffmpeg is a must for manipulating the video you want to add! You can get it from this link: https://www.gyan.dev/ffmpeg/builds/

- Bear in mind you will need a file archive editing tool, like 7Zip or WinRAR to extract the archive - the only thing you'll need is the ffmpeg.exe file from the bin folder! 

I recommend moving these to a temporary folder - in the File Explorer search bar type in "cmd" to open command prompt at that location!

These tools are used entirely through command prompt, and they are very powerful! Here are the commands you will need:

- Downloads the supplied YouTube link as an mp4 file, just replace the link in the quotes with the one you want!

  `yt-dlp.exe "https://www.youtube.com/watch?v=dQw4w9WgXcQ" -f mp4`

- Converts the supplied video into an audio wav file, which is the format Rain World uses!  

  `ffmpeg.exe -i "video.mp4" "music.wav"`

- Converts the supplied video to a certain size ("480:360" converts the video to a 480x360 pixel size)  
This is necessary to shrink large videos down so that they both fit in Pebbles' chamber, and do not cause the game to run out of memory!  
Both videos I tested with were scaled to 480x360, if the png files generated are especially large, you might run into issues!

  `ffmpeg.exe -i "video.mp4" -vf scale="480:360" "output.mp4"`

- BE CAREFUL WITH THIS ONE! Extracts all the frames of the supplied video (here "video.mp4") TO THE FOLDER IT IS RUN IN  
Don't do this on your desktop for example, unless you want thousands of pngs and a huge mess to clean up! Make a temporary folder!  

  `ffmpeg.exe -i video.mp4 %08d.png`

Finally, you can add them to the DLL and compile: download the source code from github, and replace all of the pngs in the Frames folder with the pngs you extracted from your video!

Replace the BadAppleMusic.wav file in the Sounds folder with your wav file, just make sure you rename it to BadAppleMusic.wav!

Open the .sln file in VS, and build the .dll! Hopefully it should now be able to be installed and will play your video!

There are also a bunch of const parameters in the mod itself you can mess around with, the comments should explain what most of them do!  
All the important ones are found in the VideoPlayer script!

- Don't forget to adjust the FINAL_FRAME and FRAME_RATE parameters in VideoPlayer to match your video!

If you need any help, or want me to convert a certain video for you, feel free to ask!

