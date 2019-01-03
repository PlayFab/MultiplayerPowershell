namespace PFMultiplayerCmdlets
{
    using System;
    using System.Diagnostics;
    using System.Management.Automation;
    using System.Threading;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsLifecycle.Enable, "PFMultiplayerServer")]
    public class EnablePFMultiplayerServer : PFBaseCmdlet
    {
        private const int StatusCheckIntervalSeconds = 10;

        protected override void ProcessRecord()
        {
            PlayFabMultiplayerAPI.EnableMultiplayerServersForTitleAsync(new EnableMultiplayerServersForTitleRequest()).Wait();
            TitleMultiplayerServerEnabledStatus status = GetStatus();
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (status != TitleMultiplayerServerEnabledStatus.Enabled)
            {
                Thread.Sleep(TimeSpan.FromSeconds(StatusCheckIntervalSeconds));
                WriteObject($"Querying status... Elapsed seconds : {stopwatch.Elapsed.TotalSeconds}");
                status = GetStatus();
            }

            WriteVerbose("Title is enabled for multiplayer servers.");
        }

        private TitleMultiplayerServerEnabledStatus GetStatus()
        {
            return CallPlayFabApi( () => PlayFabMultiplayerAPI.GetTitleEnabledForMultiplayerServersStatusAsync(new GetTitleEnabledForMultiplayerServersStatusRequest()))
                .Status.Value;
        }
    }
}