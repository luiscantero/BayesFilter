using Microsoft.VisualStudio.TestTools.UnitTesting;
using BayesFilter.Portable;

namespace BayesFilter.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestTrainGood()
        {
            using (var bayes = new BFEngine())
            {
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");
                bayes.TrainAsGood($"a b c d e f g h i j k l m n o p");

                Assert.AreEqual(2, bayes.GoodEventCount);
                Assert.AreEqual(0, bayes.BadEventCount);
                Assert.AreEqual(16, bayes.TokenCount);
            }
        }

        [TestMethod]
        public void TestTrainBad()
        {
            using (var bayes = new BFEngine())
            {
                bayes.TrainAsBad("q r s t u v w x y z");
                bayes.TrainAsBad("q r s t u v w x y z");

                Assert.AreEqual(0, bayes.GoodEventCount);
                Assert.AreEqual(2, bayes.BadEventCount);
                Assert.AreEqual(10, bayes.TokenCount);
            }
        }

        [TestMethod]
        public void TestEvaluate()
        {
            using (var bayes = new BFEngine())
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

                Assert.AreEqual(bayes.GoodEventCount, 5);
                Assert.AreEqual(bayes.BadEventCount, 5);
                Assert.AreEqual(bayes.TokenCount, 26);

                string test = "a";
                Assert.AreEqual(0, bayes.GetBadProbability(test));

                test = "z";
                Assert.AreEqual(1, bayes.GetBadProbability(test));

                test = "a z";
                Assert.AreEqual(0.5, bayes.GetBadProbability(test));

                test = "a b z";
                double val = bayes.GetBadProbability(test);
                Assert.IsTrue(val > 0.3 && val < 0.4); // 0.3...

                test = "a y z";
                val = bayes.GetBadProbability(test);
                Assert.IsTrue(val > 0.6 && val < 0.7); // 0.6...
            }
        }
    }
}
