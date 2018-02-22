<Query Kind="Program">
  <Namespace>EllieMae.Encompass.Query</Namespace>
  <Namespace>EllieMae.Encompass.BusinessObjects.Loans</Namespace>
  <Namespace>EllieMae.Encompass.Collections</Namespace>
</Query>

void Main()
{
  var Factory = SessionFactory.Production;

	//Do I want to modify the loans or just look at fields
	var dryrun = false;
	//Controls the way in which the criteria is built
	//There's a hard limit to the number of loans so don't try more than 350
	//or you'll get a SQLException about the query being too complex
	var limit = 50;
	//Controls the run number
	var r = 0;

	//Fields I want to look at
	var fieldIds = new[] {
		"GUID",
		"Fields.364",
		"Fields.LPID",
		"Fields.LOID",
		"LoanFolder",
		"Log.MS.LastCompleted"
	};

	var loanNumbers = new []{
		"ValidLoanNumberHere"
		};

	using (var session = Factory.Create())
	{
		while (r * limit < loanNumbers.Count())
		{
			QueryCriterion criterion = null;

			//Take the first batch(limit) of loans from the list skipping previous runs (r)
			foreach (var n in loanNumbers.Skip(r * limit).Take(limit))
			{
				if (criterion == null)
				{
					criterion = new StringFieldCriterion("Fields.364", n, StringFieldMatchType.Exact, true);
				}
				else
				{
					criterion = criterion.Or(new StringFieldCriterion("Fields.364", n, StringFieldMatchType.Exact, true));
				}
			}

			var ids = session.Loans.Query(criterion).OfType<LoanIdentity>();

			foreach (LoanIdentity id in ids)
			{
				//If all I want to do is look at some information, no need to open the loan
				//just dump the fields I want
				var fields = session.Loans.SelectFields(id.Guid, new StringList(fieldIds));

				if (!dryrun)
				{
					try
					{
						var loan = session.Loans.Open(id.Guid);
						// Do your loan field modification here
						// Don't forget to lock/unlock
						loan.Close();
					}
					catch (ArgumentException ex)
					{
						Debug.WriteLine($"{ex.Message}");
					}
				}
				else
				{
					fields.Dump();
				}
			}

			r++;
		}
	}
}
