//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Activities.Types;

//namespace GhostfolioSidekick.GhostfolioAPI.Strategies
//{
//	public class StockSplitStrategy : IHoldingStrategy
//	{
//		public int Priority => (int)StrategiesPriority.StockSplit;

//		public Task Execute(Holding holding)
//		{
//			if (holding.SymbolProfile == null)
//			{
//				return Task.CompletedTask;
//			}

//			var splits = holding.Activities.Where(x => x is StockSplitActivity).ToList();
//			if (splits.Count == 0)
//			{
//				return Task.CompletedTask;
//			}

//			foreach (var split in splits)
//			{
//				var splitActivity = (StockSplitActivity)split;
//				var splitFactor = splitActivity.FromAmount / (decimal)splitActivity.ToAmount;
//				var inverseSplitFactor = splitActivity.ToAmount / (decimal)splitActivity.FromAmount;

//				foreach (var activity in holding.Activities.Where(x => x.Date < splitActivity.Date).Select(x => x as BuySellActivity).Where(x => x != null))
//				{
//					activity!.UnitPrice = activity!.UnitPrice?.Times(splitFactor);
//					activity!.Quantity = activity!.Quantity * inverseSplitFactor;
//				}
//			}

//			return Task.CompletedTask;
//		}
//	}
//}
