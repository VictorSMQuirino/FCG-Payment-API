namespace FCG_Payments.Domain.DTO;

public class GrantGameAccessData
{
	public string UserId { get; set; }
	public string GameId { get; set; }

	public GrantGameAccessData() { }

	public GrantGameAccessData(string userId, string gameId)
	{
		UserId = userId;
		GameId = gameId;
	}
}
