namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface ITestContextService
    {
        bool IsTest { get; }
    }

    public class TestContextService : ITestContextService
    {
        public bool IsTest { get; set; }
    }
}
