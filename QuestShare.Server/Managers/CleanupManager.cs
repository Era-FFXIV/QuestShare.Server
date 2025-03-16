using QuestShare.Server.Models;

namespace QuestShare.Server.Managers
{
    public class CleanupManager
    {
        // Cleans up old database entries
        private static int CleanupTimeMinutes = 60;
        private static Timer Timer = null!;

        public void Initialize()
        {
            // Set up a timer to run every hour
            Timer = new Timer(new TimerCallback(CleanupTask), dueTime: 1000, period: CleanupTimeMinutes*60*1000, state: null);
        }

        public void Dispose()
        {
            Timer.Dispose();
        }

        private static void CleanupTask(object? timerState)
        {
            using var shareContext = new QuestShareContext();
            var toDelete = shareContext.Sessions.Where(s => s.LastUpdated < DateTime.Now.AddMinutes(-CleanupTimeMinutes)).ToList();
            Console.WriteLine($"Deleting {toDelete.Count} old shares.");
            shareContext.Sessions.RemoveRange(toDelete);
            shareContext.SaveChanges();
        }
    }
}
