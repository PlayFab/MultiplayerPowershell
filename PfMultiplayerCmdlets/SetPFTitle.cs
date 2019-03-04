namespace PFMultiplayerCmdlets
{
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Set, "PFTitle")]
    public class SetPFTitle : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string SecretKey { get; set; }

        [Parameter(Mandatory = true)]
        public string TitleId { get; set; }

        protected override void ProcessRecord()
        {
            PFTokenUtility.Instance.SetTitleSecretKey(TitleId, SecretKey);
            PFTokenUtility.Instance.GetPFTitleEntityToken();
        }
    }
}