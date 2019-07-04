using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.Election;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public class TokenTestBase : EconomicSystemTestBase
    {
        protected ECKeyPair AnnounceElectionKeyPair => SampleECKeyPairs.KeyPairs[81];
        internal ElectionContractContainer.ElectionContractStub tokenTestElectionContractStub { get; set; }
    }

    public class TokenTest : TokenTestBase
    {
        public TokenTest()
        {
            InitializeContracts();
            tokenTestElectionContractStub =
                GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress,
                    AnnounceElectionKeyPair);

            var issueResult = AsyncHelper.RunSync(() => EconomicContractStub.IssueNativeToken.SendAsync(
                new IssueNativeTokenInput
                {
                    Amount = 1000_000_00000000L,
                    To = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                    Memo = "Used to transfer other testers"
                }));
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Token_Lock()
        {
            await tokenTestElectionContractStub.AnnounceElection.SendAsync(new Empty());
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(1000_000_00000000 - 100_000_00000000);
        }

        [Fact]
        public async Task Token_Unlock()
        {
            Token_Lock();
            var address = AnnounceElectionKeyPair.PublicKey;
            await tokenTestElectionContractStub.QuitElection.SendAsync(new Empty());
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(1000_000_00000000L);
        }

        [Fact]
        public async Task SetResourceTokenUnitPrice()
        {
            var result = await TokenContractStub.SetResourceTokenUnitPrice.SendAsync(
                new SetResourceTokenUnitPriceInput()
                {
                    NetUnitPrice = 10L,
                    CpuUnitPrice = 10L,
                    StoUnitPrice = 10L
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}