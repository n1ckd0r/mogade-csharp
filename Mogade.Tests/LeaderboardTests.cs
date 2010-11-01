using Mogade.Leaderboard;
using NUnit.Framework;

namespace Mogade.Tests
{
   public class LeaderboardTests : BaseFixture
   {
      [Test]
      public void SendsDataLessScoreToTheServer()
      {
         Server.Stub(new ApiExpectation { Method = "PUT",  Url = "/scores", Request = @"{""leaderboard_id"":""mybaloney"",""score"":{""username"":""Scytale"",""points"":10039},""key"":""thekey"",""v"":1,""sig"":""2fb1a639de341453bd41219383f4b279""}"});
         var score = new Score {Points = 10039, UserName = "Scytale"};
         new Mogade("thekey", "sssshh").SaveScore("mybaloney", score);
      }

      [Test]
      public void RetrievesAllTheRanksFromTheResponse()
      {
         Server.Stub(new ApiExpectation { Response = @"{""daily"": 20, ""weekly"": 25, ""overall"": 45}" });
         var score = new Score { Points = 10039, UserName = "Scytale" };
         var ranks = new Mogade("thekey", "sssshh").SaveScore("mybaloney", score);
         Assert.AreEqual(20, ranks.Daily);
         Assert.AreEqual(25, ranks.Weekly);
         Assert.AreEqual(45, ranks.Overall);
      }

      [Test]
      public void RetrievesAnEmptyRankSet() //SaveScore isn't guaranteed to return all, or even any rank
      {
         Server.Stub(new ApiExpectation { Response = @"{}" });
         var score = new Score { Points = 10039, UserName = "Scytale" };
         var ranks = new Mogade("thekey", "sssshh").SaveScore("mybaloney", score);
         Assert.AreEqual(0, ranks.Daily);
         Assert.AreEqual(0, ranks.Weekly);
         Assert.AreEqual(0, ranks.Overall);
      }

      [Test]
      public void RetrievesAnPartialRankSet() //SaveScore isn't guaranteed to return all, or even any rank
      {
         Server.Stub(new ApiExpectation { Response = @"{""weekly"": 49494}" });
         var score = new Score { Points = 10039, UserName = "Scytale" };
         var ranks = new Mogade("thekey", "sssshh").SaveScore("mybaloney", score);
         Assert.AreEqual(0, ranks.Daily);
         Assert.AreEqual(49494, ranks.Weekly);
         Assert.AreEqual(0, ranks.Overall);
      }
   }
}