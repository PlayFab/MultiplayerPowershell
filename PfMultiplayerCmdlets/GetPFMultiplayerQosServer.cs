namespace PFMultiplayerCmdlets
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Get, "PFMultiplayerQosServer")]
    [OutputType(typeof(List<QosServer>))]
    public class GetPFMultiplayerQosServer : Cmdlet
    {
        protected override void ProcessRecord()
        {
            ListQosServersResponse response = PlayFabMultiplayerAPI.ListQosServersAsync(new ListQosServersRequest()).Result.Result;
            WriteObject(response.QosServers);
        }
    }
}