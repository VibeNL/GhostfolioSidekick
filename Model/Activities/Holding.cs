//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.Model.Activities
//{
//	public class Holding
//	{
//		internal Holding()
//		{
//			// EF Core
//		}

//		public Holding(SymbolProfile? symbolProfile)
//		{
//			SymbolProfile = symbolProfile;
//		}

//		public int Id { get; set; }

//		public SymbolProfile? SymbolProfile { get; }

//		public virtual List<Activity> Activities { get; set; } = [];

//		override public string ToString()
//		{
//			return $"{SymbolProfile?.Symbol} - {Activities.Count} activities";
//		}
//	}
//}
