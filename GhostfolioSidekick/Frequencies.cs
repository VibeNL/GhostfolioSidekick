﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick
{
	internal static class Frequencies
	{
		public static TimeSpan Hourly => TimeSpan.FromHours(1);
		public static TimeSpan Daily => TimeSpan.FromDays(1);
	}
}
