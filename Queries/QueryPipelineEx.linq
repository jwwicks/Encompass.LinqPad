<Query Kind="Program">
  <Namespace>EllieMae.Encompass.Query</Namespace>
  <Namespace>EllieMae.Encompass.BusinessObjects.Loans</Namespace>
  <Namespace>EllieMae.Encompass.BusinessObjects.Users</Namespace>
  <Namespace>EllieMae.Encompass.Collections</Namespace>
  <Namespace>EllieMae.Encompass.BusinessEnums</Namespace>
  <Namespace>EllieMae.Encompass.BusinessObjects.Loans.Logging</Namespace>
</Query>

void Main()
{
	/*
		This sample query dumps the Funding Fees for the previous days funded loans using QueryPipelineEx
	*/
	var now = DateTime.Now;
	// The following date time is more precise than actually needed
	var startDate = now.Add(new TimeSpan(-1, -now.Hour, -now.Minute, -now.Second, -now.Millisecond));
	var endDate = startDate.Add(new TimeSpan(-2, 59, 59, 59, 999));
	
	now.Dump("Start Run");
	
	var Factory = SessionFactory.Staging;

	var criterion = new StringFieldCriterion("Fields.Log.MS.LastCompleted", "Funded", StringFieldMatchType.Contains, true)
			.And(
				new DateFieldCriterion("Fields.1997", startDate, OrdinalFieldMatchType.GreaterThanOrEquals, DateFieldMatchPrecision.Exact)
				.And(new DateFieldCriterion("Fields.1997", endDate, OrdinalFieldMatchType.LessThanOrEquals, DateFieldMatchPrecision.Exact)
			));
		

	var fieldIds = new[] {
		"GUID",
		"Fields.364",
		"Fields.1997",
	};

	using (var session = Factory.Create())
	{
		using (var cursor = session.Loans.QueryPipelineEx(
				criterion,
				new SortCriterionList(new List<SortCriterion>() { new SortCriterion("Fields.1997"), new SortCriterion("Loan.LastModified")})))
		{
			try
			{
				var matched = new List<LoanDto>();
				var dto = new LoanDto();
				foreach (PipelineData data in cursor.OfType<PipelineData>())
				{
					var fields = session.Loans.SelectFields(data.LoanIdentity.Guid, new StringList(fieldIds));
					Loan l = session.Loans.Open(data.LoanIdentity.Guid);

					dto.Guid = fields[0];
					dto.LoanNumber = fields[1];
					dto.FundedDate = fields[2];
					dto.Fees = l.GetFundingFees(true).OfType<FundingFee>().Where(f => f.BalanceChecked == true);
					matched.Add(dto);
					
					l.Close();
				}
				
				matched.Dump($"Funded loan between {startDate} and {endDate}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Exception: {0}, {1}", ex.Message, ex.InnerException.Message);
			}
		}
	}
	
	DateTime.Now.Dump("End Run");
}

public class LoanDto
{
	public string Guid { get; set;}
	public string LoanNumber {get; set;}
	public string FundedDate {get; set;}
	public IEnumerable<FundingFee> Fees {get; set;}
	
	public LoanDto()
	{
		
	}
	
	public LoanDto(string guid, string loanNumber, string fundedDate, IEnumerable<FundingFee> fees)
	{
		Guid = guid;
		LoanNumber = loanNumber;
		FundedDate = fundedDate;
		Fees = fees;		
	}	
}