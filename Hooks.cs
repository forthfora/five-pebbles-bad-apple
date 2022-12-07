using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static SLOracleBehaviorHasMark;

namespace FivePebblesBadApple
{
    public static class Hooks
    {
        public static void ApplyHooks()
        {
            // Rain World Startup
            On.RainWorld.Start += RainWorldStartHook;

            // When the palette (colour) of pearls is being applied
            On.DataPearl.ApplyPalette += DataPearlApplyPaletteHook;

            // When a room is loaded
            On.Room.Loaded += RoomLoadedHook;

            // When Hunter mode is started
            On.HardmodeStart.Update += HardmodeStartUpdateHook;

            // When the game is unpaused
            On.RainWorldGame.ContinuePaused += ContinuePausedHook;

            //ProjectedImage constructor hook for hiding LoadFile()
            On.ProjectedImage.ctor += ProjectedImageCtorHook;

            // Five Pebbles Constructor Hook
            On.SSOracleBehavior.ctor += SSOracleBehaviorCtorHook;

            // Five Pebbles Update
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;

            // Moon Pearl Dialogue
            On.SLOracleBehaviorHasMark.GrabObject += SLOracleBehaviorHasMarkGrabObjectHook;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += SLOracleBehaviorHasMarkMoonConversationAddEventsHook;
        }

        private static void RainWorldStartHook(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);

            // Load the frames of the video before the game starts - these are not textures, but simply byte arrays that represent them
            // If we were to load the full textures all at once, the game would run out of memory and crash
            FivePebblesBadApple.SELF.Logger_p.LogInfo("Loading Frames...");
            FivePebblesBadApple.LoadFrames();
            FivePebblesBadApple.SELF.Logger_p.LogInfo("Finished Loading Frames!");
        }

        private static void DataPearlApplyPaletteHook(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);

            if ((self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType == EnumExt_FPBA.BadApple_Pearl)
            {
                self.color = Color.red;
                self.highlightColor = Color.green;
            }
        }

        // Credit to woutkolkman again for this!
        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);

            // Place the pearl in Pebbles' chamber, unless the player is on Hunter difficulty
            if (self.game != null && self.roomSettings != null && self.roomSettings.name.Equals("SS_AI") && firsttime &&
                (self.game.Players[0].realizedCreature as Player).slugcatStats.name != SlugcatStats.Name.Red)
            {
                // Get existing coordinate from a random object in Pebbles' chamber
                WorldCoordinate coord = self.GetWorldCoordinate(self.roomSettings.placedObjects[UnityEngine.Random.Range(0, self.roomSettings.placedObjects.Count - 1)].pos);

                // Create the pearl
                DataPearl.AbstractDataPearl pearl = new DataPearl.AbstractDataPearl(self.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                    new WorldCoordinate(self.abstractRoom.index, -1, -1, 0), self.game.GetNewID(), -1, -1, null, EnumExt_FPBA.BadApple_Pearl);
                self.abstractRoom.entities.Add(pearl);

                FivePebblesBadApple.SELF.Logger_p.LogInfo("Added FPBA_Pearl at " + coord.SaveToString() + " in SS_AI");
            }
        }

        private static bool isHunterGivenPearl = false;

        // Hunter's pearl is replaced with the Bad Apple one
        private static void HardmodeStartUpdateHook(On.HardmodeStart.orig_Update orig, HardmodeStart self, bool eu)
        {
            orig(self, eu);

            // Hunter is not given the pearl when the game first starts
            if (self.phase == HardmodeStart.Phase.Init) isHunterGivenPearl = false;

            // If the game starting phase is ending the hunter has not been given the pearl yet, that is when the recieve it
            if (self.phase == HardmodeStart.Phase.End && !isHunterGivenPearl)
            {
                isHunterGivenPearl = true;

                // Place the pearl in Hunter's stomach
                self.player.objectInStomach = new DataPearl.AbstractDataPearl(self.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                    new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0), self.room.game.GetNewID(), -1, -1, null, EnumExt_FPBA.BadApple_Pearl);

                FivePebblesBadApple.SELF.Logger_p.LogInfo("Replaced Hunter's stomach pearl with the BadApple pearl!");
            }
        }

        // We need to know when the game is unpaused to account for the passage of time while the video isn't being played, due to how the frameskip works
        // Otherwise the video will skip forward when we unpause, as if the video wasn't paused in the first place!
        private static void ContinuePausedHook(On.RainWorldGame.orig_ContinuePaused orig, RainWorldGame self)
        {
            orig(self);
            VideoPlayer.frameTimer = Time.time;
        }

        // ProjectedImage constructor hook for hiding LoadFile() (function cannot be overridden or hidden for ProjectedImage class)
        static void ProjectedImageCtorHook(On.ProjectedImage.orig_ctor orig, ProjectedImage self, List<string> imageNames, int cycleTime)
        {
            //remove LoadFile() call from constructor, so no .PNG file is required
            if (self is ProjectedImageFromMemory)
            {
                self.imageNames = imageNames;
                self.cycleTime = cycleTime;
                self.setAlpha = new float?(1f);
                return;
            }
            orig(self, imageNames, cycleTime);
        }

        // Reset variables (shouldn't have made everything static!)
        private static void SSOracleBehaviorCtorHook(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            currentState = PebblesState.None;
            badApplePearl = null;
            VideoPlayer.ResetVideo(self);

            FivePebblesBadApple.SELF.Logger_p.LogInfo("Reset Video!");
        }

        private static PebblesState currentState = PebblesState.None;

        private enum PebblesState
        {
            None,

            WatchVideo,
            GetPearl,
            ReadPearl,
            Dissapointment
        }

        private static DataPearl badApplePearl;

        // How fast the pearl travels to Pebbles' hand
        private const float PEARL_FLY_SPEED = 0.1f;

        // How long we wait between each line of dialog
        private const float DIALOGUE_DELAY = 5.0f;

        // Stores the time at which a line was said
        private static float dialogueTimer;

        // Keeps track of which line of dialogue we are showing
        private static int currentDialogue = 0;

        // Used to gradually fade the palette's colour
        private static float fadePalette = 0.0f;

        private static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            // Do not interrupt these actions
            if (self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior ||
                self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight ||
                self.action == SSOracleBehavior.Action.General_GiveMark ||
                self.conversation == null) return; // If there's no conversation, dialog interrupts will freeze the game

            // Wait until Pebbles' has finished giving slugcat the mark, unless they already have the mark
            if (self.timeSinceSeenPlayer <= 300 && !self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark) return;

            if (currentState == PebblesState.WatchVideo)
            {
                PutPearlInPebblesHand(self);

                if (VideoPlayer.isVideoFinished)
                {
                    currentState = PebblesState.Dissapointment;
                    currentDialogue = 0;
                    dialogueTimer = Time.time;
                }
                else
                {
                    VideoPlayer.PlayVideo(self);
                }
            }

            // This is the worst implementation of timed dialogue you will ever see, for your sake, please do not use this
            else if (currentState == PebblesState.ReadPearl)
            {
                PutPearlInPebblesHand(self);

                if (Time.time - dialogueTimer <= DIALOGUE_DELAY) return;

                switch (currentDialogue)
                {
                    case 0:
                        self.dialogBox.Interrupt(self.Translate("Wait... a cure for my rot? Has he finally made himself useful?!"), 10);
                        break;

                    case 1:
                        self.dialogBox.Interrupt(self.Translate("I must watch this video at once!"), 10);
                        break;

                    case 2:
                        self.dialogBox.Interrupt(self.Translate("Man, I just can't wait to find out h-"), 0);
                        currentState = PebblesState.WatchVideo;
                        break;
                }

                dialogueTimer = Time.time;
                currentDialogue += 1;
            }

            // Ditto + code repetition!
            else if (currentState == PebblesState.Dissapointment)
            {
                // Change Palette
                for (int n = 0; n < self.oracle.room.game.cameras.Length; n++)
                {
                    if (self.oracle.room.game.cameras[n].room == self.oracle.room && !self.oracle.room.game.cameras[n].AboutToSwitchRoom)
                    {
                        if (fadePalette > 0.0f) fadePalette -= 0.05f;
                        if (fadePalette < 0.0f) fadePalette = 0.0f;
                        self.oracle.room.game.cameras[n].ChangeBothPalettes(25, 26, fadePalette);
                    }
                }

                if (Time.time - dialogueTimer <= DIALOGUE_DELAY) return;

                switch (currentDialogue)
                {
                    case 0:
                        self.dialogBox.Interrupt(self.Translate("..."), 10);
                        break;

                    case 1:
                        self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                        FivePebblesBadApple.SELF.Logger_p.LogInfo("Kill slugcat for NSH's crimes!");
                        return;
                }

                dialogueTimer = Time.time;
                currentDialogue += 1;
            }

            // Before the video has started
            else if (!VideoPlayer.isVideoStarted)
            {
                // If we haven't found the pearl yet, we look for it
                // Only when the pearl is found does the video take effect
                if (badApplePearl == null)
                {
                    for (int i = 0; i < self.oracle.room.updateList.Count; i++)
                    {
                        // If the object is not a pearl we skip it
                        if (self.oracle.room.updateList[i] is not DataPearl) continue;

                        // If it is a pearl, we have to verify its type
                        if ((self.oracle.room.updateList[i] as DataPearl).AbstractPearl.dataPearlType == EnumExt_FPBA.BadApple_Pearl)
                        {
                            badApplePearl = self.oracle.room.updateList[i] as DataPearl;
                            FivePebblesBadApple.SELF.Logger_p.LogInfo("Found Bad Apple Pearl in Pebbles' chamber");

                            self.conversation.paused = true;
                            self.action = EnumExt_FPBA.BadApple;
                            break;
                        }
                    }
                }

                // This is whole section is the lazy way of doing things
                if (badApplePearl == null) return;

                if (currentState != PebblesState.GetPearl && (self.oracle.room.game.Players[0].realizedCreature as Player).slugcatStats.name == SlugcatStats.Name.Red)
                {
                    self.dialogBox.Interrupt(self.Translate("Oh, what thoughtful gift has NSH sent me this time?"), 10);
                }
                currentState = PebblesState.GetPearl;

                // Force the player to release the pearl if they are holding it
                if (badApplePearl.grabbedBy.Count > 0)
                {
                    for (int i = badApplePearl.grabbedBy.Count - 1; i >= 0; i--)
                    {
                        badApplePearl.grabbedBy[i].Release();
                    }
                }

                // Get where Pebbles' hand is
                Vector2 handPositon = (self.oracle.graphicsModule as OracleGraphics).hands[1].pos;
                badApplePearl.firstChunk.vel = (handPositon - badApplePearl.firstChunk.pos) * PEARL_FLY_SPEED;

                // Once the pearl is within a certain range of Pebbles' hand, we consider him holding it
                if (Custom.DistLess(handPositon, badApplePearl.firstChunk.pos, 10.0f))
                {
                    PutPearlInPebblesHand(self);
                    currentState = PebblesState.ReadPearl;
                }
                dialogueTimer = Time.time;
                currentDialogue = 0;
            }
        }

        // Just keeps the pearl in Pebbles' hand, as well as resets its velocity to 0 to prevent gravity build up shenanigans 
        private static void PutPearlInPebblesHand(SSOracleBehavior self)
        {
            // Get where Pebbles' hand is
            Vector2 handPositon = (self.oracle.graphicsModule as OracleGraphics).hands[1].pos;
            badApplePearl.firstChunk.pos = handPositon;
            badApplePearl.firstChunk.vel = new Vector2(0.0f, 0.0f);
        }

        private static void SLOracleBehaviorHasMarkGrabObjectHook(On.SLOracleBehaviorHasMark.orig_GrabObject orig, SLOracleBehaviorHasMark self, PhysicalObject item)
        {
            if (item is DataPearl)
            {
                if ((item as DataPearl).AbstractPearl.dataPearlType == EnumExt_FPBA.BadApple_Pearl)
                {
                    Conversation.ID id = EnumExt_FPBA.BadApple_Pearl_Conversation;

                    self.currentConversation = new MoonConversation(id, self, MiscItemType.NA);
                    self.State.significantPearls[(int)(item as DataPearl).AbstractPearl.dataPearlType] = true;
                    self.State.totalPearlsBrought++;
                    Debug.Log("pearls brought up: " + self.State.totalPearlsBrought);
                    return;
                }
            }

            orig(self, item);
        }

        // Moon Dialogue
        // There might be a bug where Moon will say 'This one again?' even though she has never seen the pearl before
        // I'm too lazy to debug it though... like who will read this anyway, right? ...right?
        static void SLOracleBehaviorHasMarkMoonConversationAddEventsHook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, MoonConversation self)
        {
            orig(self);

            if (self.id == EnumExt_FPBA.BadApple_Pearl_Conversation)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Another pearl from NSH? What could this mea-"), 0));
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Ah... I see..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Yes, I think Pebbles would enjoy this!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Why don't you try giving it to him? I'm sure he'd love to play it for you!"), 0));
            }
        }
    }
}
