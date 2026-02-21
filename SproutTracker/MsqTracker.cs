using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace SproutTracker;

public class MsqTracker : IDisposable {
    private readonly ApiManager apiManager;

    public MsqTracker() {
        Services.Framework.Update += this.Update;
        this.apiManager = new ApiManager();
    }

    public void Dispose() {
        Services.Framework.Update -= this.Update;
        this.apiManager.Dispose();
    }

    private unsafe void Update(IFramework framework) {
        var scenarioTree = AgentScenarioTree.Instance();
        var questManager = QuestManager.Instance();
        var cid = Services.PlayerState.ContentId;
        if (scenarioTree == null
            || scenarioTree->Data == null
            || questManager == null
            || cid == 0
            || Services.ObjectTable.LocalPlayer == null) return;

        var oldProgression = Plugin.Configuration.QuestProgression.GetValueOrDefault(cid);

        var currentQuestId = scenarioTree->Data->MainScenarioQuestIds[0];
        var completeQuestId = scenarioTree->Data->MainScenarioQuestIds[3];
        var complete = oldProgression?.Quest == completeQuestId;

        if (complete) {
            if (oldProgression is {Complete: false}) {
                var progression = new QuestProgression(
                    oldProgression.Quest,
                    oldProgression.Sequence,
                    true
                );
                this.WriteQuestProgression(progression, cid);
            }
        } else if (currentQuestId != 0) {
            var quest = questManager->GetQuestById(currentQuestId);
            if (quest == null) return;
            var sequence = quest->Sequence;

            var progression = new QuestProgression(
                currentQuestId,
                sequence,
                false
            );

            if (!progression.Equals(oldProgression)) {
                this.WriteQuestProgression(progression, cid);
            }
        }
    }

    private void WriteQuestProgression(QuestProgression progression, ulong cid) {
        if (cid == 0) return;

        var scenarioTree = Services.DataManager.GetExcelSheet<ScenarioTree>()!;
        if (scenarioTree.TryGetRow(progression.Quest | 0x10000U, out var row)) {
            Services.PluginLog.Debug(
                "Writing quest progression: scenario tree={ScenarioTree} quest={Quest} sequence={Sequence} complete={Complete}",
                progression.Quest,
                row.RowId,
                progression.Sequence,
                progression.Complete
            );

            Plugin.Configuration.QuestProgression[cid] = progression;
            Plugin.Configuration.Save();

            var character = new Character();
            Task.Run(async () => {
                await this.apiManager.SubmitProgress(new ProgressSubmit {
                    Character = character,
                    Quest = row.RowId,
                    Sequence = progression.Sequence,
                    Complete = progression.Complete
                });
            });
        } else {
            Services.PluginLog.Warning(
                "Failed to find scenario tree row for quest {Quest}",
                progression.Quest
            );
        }
    }
}
