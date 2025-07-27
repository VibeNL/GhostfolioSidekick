using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using System.Collections.Generic;
using Xunit;
using Plotly.Blazor;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class HoldingsTests
    {
        [Fact]
        public void SortHoldings_SortsBySymbolAscendingAndDescending()
        {
            var holdings = new HoldingsTestable();
            holdings.SetHoldingsList(new List<HoldingDisplayModel>
            {
                new() { Symbol = "B", Name = "Beta" },
                new() { Symbol = "A", Name = "Alpha" },
                new() { Symbol = "C", Name = "Charlie" }
            });
            holdings.SetSortColumn("Symbol");
            holdings.SetSortAscending(true);

            holdings.InvokeSortHoldings();
            Assert.Equal(new[] { "A", "B", "C" }, holdings.GetHoldingsList().ConvertAll(h => h.Symbol));

            holdings.SetSortAscending(false);
            holdings.InvokeSortHoldings();
            Assert.Equal(new[] { "C", "B", "A" }, holdings.GetHoldingsList().ConvertAll(h => h.Symbol));
        }

        [Fact]
        public void GetColorForGainLoss_ReturnsExpectedColors()
        {
            var holdings = new HoldingsTestable();
            Assert.Equal("#808080", holdings.InvokeGetColorForGainLoss(0)); // Neutral
            Assert.StartsWith("#", holdings.InvokeGetColorForGainLoss(0.5m).ToString()); // Greenish
            Assert.StartsWith("#", holdings.InvokeGetColorForGainLoss(-0.5m).ToString()); // Reddish
        }

        [Fact]
        public void PrepareTreemapData_DoesNothingIfEmpty()
        {
            var holdings = new HoldingsTestable();
            holdings.SetHoldingsList(new List<HoldingDisplayModel>());
            holdings.InvokePrepareTreemapData();
            Assert.NotNull(holdings.GetPlotData());
            Assert.Empty(holdings.GetPlotData());
        }
    }

    // Testable subclass using reflection to access private members
    public class HoldingsTestable : Holdings
    {
        public void SetHoldingsList(List<HoldingDisplayModel> value)
        {
            typeof(Holdings).GetField("HoldingsList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(this, value);
        }
        public List<HoldingDisplayModel> GetHoldingsList()
        {
            return (List<HoldingDisplayModel>)typeof(Holdings).GetField("HoldingsList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(this)!;
        }
        public void SetSortColumn(string value)
        {
            typeof(Holdings).GetField("sortColumn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(this, value);
        }
        public void SetSortAscending(bool value)
        {
            typeof(Holdings).GetField("sortAscending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(this, value);
        }
        public void InvokeSortHoldings()
        {
            typeof(Holdings).GetMethod("SortHoldings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(this, null);
        }
        public object InvokeGetColorForGainLoss(decimal p)
        {
            return typeof(Holdings).GetMethod("GetColorForGainLoss", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(this, new object[] { p })!;
        }
        public void InvokePrepareTreemapData()
        {
            typeof(Holdings).GetMethod("PrepareTreemapData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(this, null);
        }
        public IList<ITrace> GetPlotData()
        {
            return (IList<ITrace>)typeof(Holdings).GetField("plotData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(this)!;
        }
    }
}
