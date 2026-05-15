using Election.VoteVault.Models;

namespace Election.VoteVault.Interfaces;

public interface IVoteVaultService
{
    CustodiedVote CustodyVote(string payload);

    Task<CustodiedVote> CustodyVoteAsync(
        string payload,
        string terminalId,
        string sessionId,
        CancellationToken cancellationToken = default
    );

    int GetCustodiedVotesCount();

    IEnumerable<CustodiedVote> GetVotes();

    IEnumerable<CustodiedVote> GetCustodiedVotesInternal();
}