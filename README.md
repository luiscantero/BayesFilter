# BayesFilter
## Description
* Visual Studio 2019 C# project with Bayesian filter implemented as a .NET Standard library.

## Important
* The program needs to be trained before it can classify text in good or bad.
* Only words that occur at least 5 times in different texts (default setting) are considered.
* If there isn't enough information to classify a text, the default probability will be 50% (`0.5`).
* When using the program with Emails, include the full headers.

## Features
* Only considers the `n` most significant words (n-gram), measured by their probability of being spam. This is to avoid spammers filling the message with innocent words to decrease the spam probability.
* Only considers words that occured a `y` minimum amount of times. This is to avoid spammers filling the message with unknown (random) words to decrease the spam probability.
* Auto-learning: it learns only if all `x` significant words that occur at least `y` times have been found. This is to avoid the auto learning feature from spoiling the database by learning words wrongly.

## Reference
* [A Plan for Spam](http://www.paulgraham.com/spam.html)
* [Better Bayesian Filtering](http://www.paulgraham.com/better.html)
* [Ending Spam: Bayesian Content Filtering and the Art of Statistical Language Classification
ISBN: 1593270526](http://www.amazon.com/dp/1593270526)
* [SpamBayes](http://spambayes.sourceforge.net/)

### License
[MIT](http://opensource.org/licenses/MIT)
