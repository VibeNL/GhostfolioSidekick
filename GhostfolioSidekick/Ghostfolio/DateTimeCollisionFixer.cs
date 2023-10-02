using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.Ghostfolio
{
	public class DateTimeCollisionFixer
	{
		public static IEnumerable<Order> Fix(IEnumerable<Order> orders)
		{
			var isChecked = false;

			var updated = orders.ToList();
			while (!isChecked)
			{
				isChecked = true;

				updated = updated
					.GroupBy(x => new { x.Asset?.Symbol, x.Date })
					.SelectMany(x => x.OrderBy(y => y.ReferenceCode).Select((y, i) =>
					{
						if (i > 0)
						{
							isChecked = false;
						}

						y.Date = y.Date.AddSeconds(i);
						return y;
					})).ToList();
			}

			return updated;
		}
	}
}
