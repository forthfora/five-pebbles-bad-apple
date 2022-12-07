using MonoMod.Cil;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Huge thanks to Slime_Cubed for sending me this code, as well as Wack and Bro for helping me!
// The following is the guide on how to use this system, it was not created in any part by me!
/*

Hi!
This is a quick-and-dirty system to load in custom sounds.

To use it, add a Sounds folder to your project's root and put your .wav files in it. Import them into
the project, then right click, go to properties, and set their build actions to "Embedded Resource".
Once you have all the clips you want, head over to EnumExt_Snd and add in your sounds IDs. Note that
sound IDs are not necessarily the same as your .wav files, you can name them whatever you want.
After adding the sound ID, add a line to the array below them to indicate which clips it should play.

Adding the hooks looks like: CustomSounds.ApplyHooks();
Playing a custom sound looks like: room.PlaySound(EnumExt_Snd.MySound, someBodyChunk);

*/

namespace FivePebblesBadApple
{
    // Enum Extender Dependency
    public static class EnumExt_Snd
    {
        public static SoundID BadAppleMusic;
        internal static readonly string[] soundLines = {
            $"{nameof(BadAppleMusic)}/dopplerFac=0 : BadAppleMusic/vol=0.5",
        };
    }

    internal static class CustomSounds
    {
        public static void ApplyHooks()
        {
            On.SoundLoader.CheckIfFileExistsAsUnityResource += SoundLoader_CheckIfFileExistsAsUnityResource;
            On.SoundLoader.GetAudioClip += SoundLoader_GetAudioClip;
            IL.SoundLoader.LoadSounds += SoundLoader_LoadSounds;
        }

        // Load custom audio clip from embedded resources
        private static AudioClip SoundLoader_GetAudioClip(On.SoundLoader.orig_GetAudioClip orig, SoundLoader self, int i)
        {
            var name = self.audioClipNames[i];
            if (self.audioClipsThroughUnity[i] && self.unityAudio[i].Any(x => x == null) && WAV.HasEmbedded(name))
            {
                if (self.soundVariations[i] == 1)
                {
                    self.unityAudio[i][0] = WAV.GetEmbedded(name);
                }
                else
                {
                    for (int num = 0; num < self.soundVariations[i]; num++)
                        self.unityAudio[i][num] = WAV.GetEmbedded($"{name}_{num + 1}");
                }
            }

            return orig(self, i);
        }

        private static void SoundLoader_LoadSounds(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After, x => x.MatchCall(typeof(File), "ReadAllLines"));
            c.EmitDelegate<Func<string[], string[]>>(lines =>
            {
                int lastLine = lines.Length;
                Array.Resize(ref lines, lines.Length + EnumExt_Snd.soundLines.Length);
                EnumExt_Snd.soundLines.CopyTo(lines, lastLine);
                return lines;
            });
        }

        private static bool SoundLoader_CheckIfFileExistsAsUnityResource(On.SoundLoader.orig_CheckIfFileExistsAsUnityResource orig, SoundLoader self, string name)
        {
            return orig(self, name) || WAV.HasEmbedded(name);
        }
    }
}