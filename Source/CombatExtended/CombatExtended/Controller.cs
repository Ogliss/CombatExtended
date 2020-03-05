using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.HarmonyCE;

namespace CombatExtended
{
    public class Controller : Mod
    {
        public static Settings settings;

        public Controller(ModContentPack content) : base(content)
        {
            Log.Message("Combat Extended :: initialing");
            settings = GetSettings<Settings>();

            Log.Message("Combat Extended :: 1");
            // Apply Harmony patches
            HarmonyBase.InitPatches();

            Log.Message("Combat Extended :: 2");
            // Initialize loadout generator
            LongEventHandler.QueueLongEvent(LoadoutPropertiesExtension.Reset, "Other def binding, resetting and global operations.", false, null);

            Log.Message("Combat Extended :: 3");
            // Inject ammo
            LongEventHandler.QueueLongEvent(AmmoInjector.Inject, "LibraryStartup", false, null);

            Log.Message("Combat Extended :: 4");
            // Inject pawn and plant bounds
            LongEventHandler.QueueLongEvent(BoundsInjector.Inject, "CE_LongEvent_BoundingBoxes", false, null);

            Log.Message("Combat Extended :: 5");

            // Tutorial popup
            if (settings.ShowTutorialPopup && !Prefs.AdaptiveTrainingEnabled)
                LongEventHandler.QueueLongEvent(DoTutorialPopup, "InitializingInterface", false, null);
            Log.Message("Combat Extended :: initialized");
        }

        private static void DoTutorialPopup()
        {
            var enableAction = new Action(() =>
            {
                Prefs.AdaptiveTrainingEnabled = true;
                settings.ShowTutorialPopup = false;
                settings.Write();
            });
            var disableAction = new Action(() =>
            {
                settings.ShowTutorialPopup = false;
                settings.Write();
            });

            var dialog = new Dialog_MessageBox("CE_EnableTutorText".Translate(), "CE_EnableTutorDisable".Translate(), disableAction, "CE_EnableTutorEnable".Translate(),
                enableAction, null, true);
            Find.WindowStack.Add(dialog);
        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
