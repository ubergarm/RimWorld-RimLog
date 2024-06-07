using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using RimWorld;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace RimLog
{
  [StaticConstructorOnStartup]
  public static class RimLogLoader
  {
    static RimLogLoader()
    {
      var harmony = new Harmony("net.ubergarm.rimworld.mods.rimlog");
      harmony.PatchAll();

      if (RimLog.Settings.isEnabled) {
        Util.Log("[RimLog] Enabled");
      } else {
        Util.Log("[RimLog] Disabled");
      }
    }
  }

  public class RimLog : Mod
  {
    public static RimLogSettings Settings;

    public RimLog(ModContentPack content) : base(content)
    {
      Settings = GetSettings<RimLogSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
      RimLogSettings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
      return "RimLog";
    }

  }

  // Utilities
  public static class Util
  {
    // FIXME: this is manual per compile for now, oof dirty.. lol.
    static bool isDebugMode = false;
    static string outputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "rimlog.txt");

    public static void Debug(string msg)
    {
      if(isDebugMode) {
        Verse.Log.Message(msg);
      }
    }

    public static void Log(string msg)
    {
      Verse.Log.Message(msg);
    }

    public static void LogToFile(string msg)
    {
      try {
        string clean = msg.StripTags();
        using (StreamWriter sw = new StreamWriter(outputFile, true, Encoding.UTF8)) {
          sw.WriteLine(clean);
        }
      }
      catch(Exception e) {
        Util.Debug($"[RimLog] ERROR writing file. Disabling Logging. {outputFile}. [{e.Source}: {e.Message}]\n\nTrace:\n{e.StackTrace}");
        RimLog.Settings.isEnabled = false;
      }
    }

    public static void LogArchivable(IArchivable archivable) {
      if(!RimLog.Settings.isEnabled) return;
      string tick = archivable?.CreatedTicksGame.ToString();
      string label = archivable?.ArchivedLabel;
      label = label.Replace(",", string.Empty);
      // string tooltip = archivable?.ArchivedTooltip;

      Util.Debug("[RimLog] Found an event at " + tick + " named: " + label);
      Util.LogToFile($"{tick},archivable,,{label}");
    }

    public static void LogEntry(LogEntry entry) {
      // TODO: add config to suppress all the missed shots as training dummy spams logs with mingun lmao
      if(!RimLog.Settings.isEnabled) return;
      string tick = entry?.Tick.ToString();
      var label = entry.ToGameStringFromPOV(entry.GetConcerns().First());
      label = label.Replace(",", string.Empty);

      Util.Debug($"[RimLog] Found LogEntry at {tick},{label}");
      Util.LogToFile($"{tick},log,{entry.def},{label}");
    }

    public static void LogTale(Tale tale) {
      if(!RimLog.Settings.isEnabled) return;
      string tick = tale?.date.ToString();
      string def = tale.def?.defName;
      string label = tale?.ShortSummary;

      switch (tale.def.defName) {
        case "MajorThreat":
        case "KilledChild":
          break;
        default:
          GrammarRequest req = default(GrammarRequest);
          req.IncludesBare.AddRange(tale.GetTextGenerationIncludes());
          req.Rules.AddRange(tale.GetTextGenerationRules(req.Constants));
          label = GrammarResolver.Resolve("tale_noun", req, tale.def.defName);

          // TODO: If any new defNames return ERR, fallback to ShortSummary (will throw error in Debug screen until added above)
          if (label.Contains("ERR")) {
            label = tale?.ShortSummary;
            Util.Log($"[RimLog] WARNING: taleDefName {def} has unsupported grammar; falling back to ShortSummary.");
            break;
          }

          // Try to add extra details (seem to be randomly generated and not actually saved)
          //string desc = GrammarResolver.Resolve("desc_sentence", req, tale.def.defName);
          //if (desc.Contains("ERR")) {
          //  break;
          //}
          //label = $"{label}. {desc}";
          //label = label.Replace(",", string.Empty);

          // Try to add extra details (seem to be randomly generated and not actually saved)
          //string phrase = GrammarResolver.Resolve("circumstance_phrase", req, tale.def.defName);
          //if (phrase.Contains("ERR")) {
          //  break;
          //}
          //label = $"{label} {phrase}.";

          // remove commas to ensure output is valid CSV file
          label = label.Replace(",", string.Empty);
          break;
      }

      Util.Debug($"[RimLog] tale tick,defName,summary: {tick},{tale.def.defName},{label}");
      Util.LogToFile($"{tick},tale,{def},{label}");
    }

    public static void LogQuest(Quest quest) {
      if(!RimLog.Settings.isEnabled) return;
      string tick = quest.EverAccepted ? quest.acceptanceTick.ToString() : quest.appearanceTick.ToString();
      string questAccepted = (quest.EverAccepted == true) ? "questAccepted" : "questNew";
      string accepterName = ((quest.AccepterPawn != null) ? quest.AccepterPawn.Name.ToStringShort : null) ?? "colony";
      string questName = quest.name;
      //string questDescription = quest.description.Resolve();

      questName = questName.Replace(",", string.Empty);

      Util.Debug($"[RimLog] Quest {quest.name}");
      LogToFile($"{tick},{questAccepted},{accepterName},{questName}");
    }
  }
}
