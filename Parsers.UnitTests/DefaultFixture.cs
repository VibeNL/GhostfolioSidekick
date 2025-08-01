﻿using AutoFixture;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public static class DefaultFixture
	{
		public static Fixture Create()
		{
			var fixture = new Fixture();
			fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));
			return fixture;
		}
	}
}
