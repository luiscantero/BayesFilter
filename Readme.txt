Important:
-	The program needs to be trained before it can classify text. Only words that occur at least 5 times in different texts (default setting) are considered.
	If there isn't enough information to classify a text, the default probability will be 50% (0.5).
-	When using the program with Emails, include the full headers.


Features:
-	Only considers the "X" most significant words, measured by their probability of being spam. This is to avoid spammers filling the message
	with innocent words to decrease the spam probability.
-	Only considers words that occured a "Y" minimum amount of times. This is to avoid spammers filling the message with unknown (random) words to decrease
	the spam probability.
-	Auto-learning: it learns only if all "X" significant words that occur at least "Y" times have been found. This is to avoid the auto learning feature
	from spoiling the database by learning words wrongly.

More information:

A Plan for Spam
http://www.paulgraham.com/spam.html

Better Bayesian Filtering
http://www.paulgraham.com/better.html

Ending Spam: Bayesian Content Filtering and the Art of Statistical Language Classification
ISBN: 1593270526
http://www.amazon.com/exec/obidos/tg/detail/-/1593270526/102-0359289-9816932

SpamBayes
http://spambayes.sourceforge.net/