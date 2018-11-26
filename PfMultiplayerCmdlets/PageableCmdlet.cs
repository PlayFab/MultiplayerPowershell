namespace PFMultiplayerCmdlets
{
    using System.Management.Automation;

    public class PageableCmdlet : PFBaseCmdlet
    {
        protected const int DefaultPageSize = 10;

        [Parameter]
        public SwitchParameter All { get; set; }
    }
}