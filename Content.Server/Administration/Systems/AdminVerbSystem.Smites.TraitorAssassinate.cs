using Content.Server.Traitor.Components; // Подключение трейтор-компоненты
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    // Smite: Traitor Assassination
    private void AddSmiteTraitorAssassinateVerb(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;
        var player = actor.PlayerSession;
        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;
        var verb = new Verb
        {
            Text = Loc.GetString("admin-smite-traitor-assassinate-name"),
            Category = VerbCategory.Smite,
            Act = () => ExecuteSmiteTraitorAssassinate(args.Target),
            Priority = 99f,
        };
        args.Verbs.Add(verb);
    }

    private void ExecuteSmiteTraitorAssassinate(EntityUid target)
    {
        // 1. Собрать всех трейторов с живым mind
        var traitors = EntityManager.EntityQuery<TraitorComponent>().ToList();
        int count = 0;
        foreach (var traitor in traitors)
        {
            var owner = traitor.Owner;
            if (owner == target)
                continue; // Не даём цель самому себе
            if (!TryComp<MindContainerComponent>(owner, out var mindContainer))
                continue;
            if (mindContainer.Mind == null)
                continue;
            // 2. Создаём цель на убийство target'а
            var objectiveId = "KillRandomPersonObjective"; // Можно завести отдельный, но этого достаточно для теста
            var objective = EntityManager.SpawnEntity(objectiveId, EntityCoordinates.Invalid);
            if (!TryComp<ObjectiveComponent>(objective, out var objComp))
                continue;
            if (!TryComp<TargetObjectiveComponent>(objective, out var targetObj))
                continue;
            // Назначаем целью выбранного entity
            targetObj.Target = target;
            // 3. Добавляем objective этому трейтору
            _mindSystem.AddObjective(mindContainer.Mind.Value, objective);
            count++;
            // Вывести popup трейтору
            _popupSystem.PopupEntity(Loc.GetString("admin-smite-traitor-assassinate-popup", ("target", target)), owner, Filter.Entities(owner));
        }
        // Popup админу итог
        _popupSystem.PopupAdmin(Loc.GetString("admin-smite-traitor-assassinate-done-popup", ("count", count)), Filter.AdminAdmins());
    }
}