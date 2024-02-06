﻿using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Compare
{
	public class MatchActivity
	{
		public required Activity Activity { get; set; }

		public bool IsMatched { get; set; }
	}
}
