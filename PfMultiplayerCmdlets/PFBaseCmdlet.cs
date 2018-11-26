namespace PFMultiplayerCmdlets
{
    using System;
    using System.Management.Automation;
    using PlayFab;

    public class PFBaseCmdlet : Cmdlet
    {
        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            {
                ThrowTerminatingError(new ErrorRecord(new Exception("Run Get-PFTitleEntityToken before running any cmdlet."), "EntityTokenMissing",
                    ErrorCategory.AuthenticationError, null));
            }
        }
    }
}