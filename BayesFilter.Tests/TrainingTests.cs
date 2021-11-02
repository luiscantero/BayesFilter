using Microsoft.VisualStudio.TestTools.UnitTesting;
using LC.BayesFilter;
using FluentAssertions;

namespace BayesFilter.Tests
{
    [TestClass]
    public class TrainingTests
    {
        [TestMethod]
        public void TestTrainGood()
        {
            using (var bayes = new BfEngine(new ConsolePlatformServices()))
            {
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");

                bayes.GoodEventCount.Should().Be(2);
                bayes.BadEventCount.Should().Be(0);
                bayes.TokenCount.Should().Be(16);
            }
        }

        [TestMethod]
        public void TestTrainBad()
        {
            using (var bayes = new BfEngine(new ConsolePlatformServices()))
            {
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");

                bayes.GoodEventCount.Should().Be(0);
                bayes.BadEventCount.Should().Be(2);
                bayes.TokenCount.Should().Be(10);
            }
        }

        [TestMethod]
        public void TestEvaluate()
        {
            using (var bayes = new BfEngine(new ConsolePlatformServices()))
            {
                // Train 5 times to reach MinTokenOccurrence.
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");

                // Train 5 times to reach MinTokenOccurrence.
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");

                bayes.GoodEventCount.Should().Be(5);
                bayes.BadEventCount.Should().Be(5);
                bayes.TokenCount.Should().Be(26);

                string test;
                double val;

                test = "a";
                bayes.GetBadProbability(test).Should().Be(0);

                test = "A";
                bayes.GetBadProbability(test).Should().Be(0);

                test = "z";
                bayes.GetBadProbability(test).Should().Be(1);

                test = "a z";
                bayes.GetBadProbability(test).Should().Be(0.5);

                test = "A Z";
                bayes.GetBadProbability(test).Should().Be(0.5);

                test = "a b z";
                val = bayes.GetBadProbability(test);
                val.Should().BeApproximately(1.0 / 3.0, double.Epsilon);

                test = "a y z";
                val = bayes.GetBadProbability(test);
                val.Should().BeApproximately(2.0 / 3.0, double.Epsilon);

                bayes.AutoTrain = true; // Only happens if result passes threshold test.
                test = "a b c d e f g h i j k l m n o p q";
                val = bayes.GetBadProbability(test);
                val.Should().BeInRange(0.06, 0.07); // 0.06...
                val = bayes.GetBadProbability(test);
                val.Should().BeInRange(0.05, 0.06); // 0.05...
                val = bayes.GetBadProbability(test);
                val.Should().BeInRange(0.04, 0.05); // 0.04...
            }
        }
    }
}
