using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Snrs.Server.Commands
{
    // Пример атрибутов — замените на реальные из проекта
    [Command("setstationrole")]
    [Help("setstationrole [station] [job] [set/adjust/infinite] [amount]\nExamples:\n  setstationrole Delta Engineer set 3\n  setstationrole Delta Engineer adjust -1\n  setstationrole Delta Engineer infinite")]
    [RequirePermission(Permission.Debug)]
    public class SetStationRoleCommand : ICommand
    {
        private readonly IStationManager _stationManager;
        private readonly IJobManager _jobManager;

        public SetStationRoleCommand(IStationManager stationManager, IJobManager jobManager)
        {
            _stationManager = stationManager;
            _jobManager = jobManager;
        }

        public async Task ExecuteAsync(ICommandContext ctx, string[] args)
        {
            if (args.Length < 3)
            {
                ctx.Reply("Usage: setstationrole [station] [job] [set/adjust/infinite] [amount]");
                return;
            }

            var stationName = args[0];
            var jobName = args[1];
            var mode = args[2].ToLowerInvariant();

            int amount = 0;
            if (mode != "infinite")
            {
                if (args.Length < 4 || !int.TryParse(args[3], out amount))
                {
                    ctx.Reply("Invalid or missing amount. Usage: setstationrole [station] [job] [set/adjust/infinite] [amount]");
                    return;
                }
            }

            var station = _stationManager.FindByName(stationName);
            if (station == null)
            {
                ctx.Reply($"Station '{stationName}' not found.");
                return;
            }

            var job = _jobManager.FindByName(jobName);
            if (job == null)
            {
                ctx.Reply($"Job/role '{jobName}' not found.");
                return;
            }

            try
            {
                switch (mode)
                {
                    case "set":
                        station.SetRoleCount(job.Id, Math.Max(0, amount));
                        ctx.Reply($"Set {job.Name} on {station.Name} to {Math.Max(0, amount)}.");
                        break;

                    case "adjust":
                        station.AdjustRoleCount(job.Id, amount);
                        var newCount = station.GetRoleCount(job.Id);
                        ctx.Reply($"Adjusted {job.Name} on {station.Name} by {amount}. New count: {newCount}.");
                        break;

                    case "infinite":
                        station.SetRoleInfinite(job.Id, true);
                        ctx.Reply($"Set {job.Name} on {station.Name} to infinite.");
                        break;

                    default:
                        ctx.Reply("Mode must be one of: set, adjust, infinite.");
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error while changing role: {ex.Message}");
            }
        }

        // Таб-Completion методы (подключите в точке регистрации команд)
        public IEnumerable<string> CompleteStation(string prefix)
        {
            return _stationManager.GetAll()
                                  .Select(s => s.Name)
                                  .Where(n => n.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> CompleteJob(string prefix)
        {
            return _jobManager.GetAll()
                              .Select(j => j.Name)
                              .Where(n => n.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> CompleteMode(string prefix)
        {
            var modes = new[] { "set", "adjust", "infinite" };
            return modes.Where(m => m.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }
    }
}