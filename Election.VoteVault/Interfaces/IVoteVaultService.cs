using Election.VoteVault.Models;

namespace Election.VoteVault.Interfaces;

public interface IVoteVaultService
{
    CustodiedVote CustodyVote(string payload);

    int GetCustodiedVotesCount();

    IEnumerable<CustodiedVote> GetVotes();

    IEnumerable<CustodiedVote> GetCustodiedVotesInternal();
}