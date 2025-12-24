namespace GhostfolioSidekick.Parsers.GoldRepublic
{
	public record GoldRepublicTransactionDetails(
		DateOnly? ExecutionDate,
		string Action,
		decimal TransactionValue,
		decimal Fee,
		decimal Volume,
		decimal Total
	);
}
