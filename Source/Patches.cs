using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using RimWorld;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace RimLog
{
  // Log everything that already happened in the save file
  [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
  static class SavedGameLoaderNow_LoadGameFromSaveFileNow_Patch
  {
    public static void Postfix()
    {
      {
        if (!RimLog.Settings.isEnabled) {
          return;
        }

        // FIXME: seems like this gets called twice when hosting MP save game and possibly other loading situations?
        Util.Log("[RimLog] Begin Processing Saved Game Logs.");
        // print a commented header as this appends to any previous log file
        string time = DateTime.Now.ToString("hh:mm:ss");
        string date = DateTime.Now.ToString("yy-MM-dd");
        string header = $"####### RimLog {date} {time} #######";
        Util.LogToFile(header);
        // messages
        Find.Archive.ArchivablesListForReading.ForEach(Util.LogArchivable);
        // tales
        Find.TaleManager.AllTalesListForReading.ForEach(Util.LogTale);
        // quests
        Find.QuestManager.QuestsListForReading.ForEach(Util.LogQuest);
        // play logs (deepthought/chitchat and battle)
        Find.PlayLog.AllEntries.ForEach(Util.LogEntry);
        Util.Log("[RimLog] End Processing Saved Game Logs.");
      }
    }
  }

  // messages
  [HarmonyPatch(typeof(Messages), "Message", new Type[]
  {
  typeof(Message),
  typeof(bool)
  })]
  static class Patch_Messages_Message
  {
    public static void Prefix(Message msg, bool historical)
    {
      Util.LogArchivable(msg);
    }
  }

  // tales
  [HarmonyPatch(typeof(TaleRecorder), nameof(TaleRecorder.RecordTale))]
  static class Patch_TaleRecorder_RecordTale
  {
    public static void Postfix(Tale __result, TaleDef def, object[] args)
    {
      if (__result == null) return;
      Util.LogTale(__result);
    }
  }

  // quests
  [HarmonyPatch(typeof(Quest), nameof(Quest.PostAdded))]
  static class Patch_Quest_PostAdded
  {
    public static void Prefix(Quest __instance)
    {
      Util.LogQuest(__instance);
    }
  }

  // Play ChitChat Logs
  [HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
  public static class Verse_PlayLog_Add
  {
    public static void Postfix(LogEntry entry) {
      Util.LogEntry(entry);
    }
  }

  // Battle Combat Logs
  [HarmonyPatch(typeof(BattleLog), nameof(BattleLog.Add))]
  public static class Verse_BattleLog_Add
  {
    public static void Postfix(LogEntry entry) {
      Util.LogEntry(entry);
    }
  }

  // TODO: trade deal details
  // TODO: whatever else interesting that can be logged and isn't already covered
}
