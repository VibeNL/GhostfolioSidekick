using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace PortfolioViewer.Pages
{
    public partial class Index
    {
        [Inject]
        public IApiWrapper ApiWrapper { get; set; }

        [Inject]
        public IDbContextFactory<DatabaseContext> DbContextFactory { get; set; }

        private List<Activity> activities;
        private List<SymbolProfile> symbolProfiles;

        protected override async Task OnInitializedAsync()
        {
            using var dbContext = await DbContextFactory.CreateDbContextAsync();
            activities = await dbContext.Activities.ToListAsync();
            symbolProfiles = await dbContext.SymbolProfiles.ToListAsync();
        }
    }
}
